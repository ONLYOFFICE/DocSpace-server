// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

using System.Threading.Channels;

using ASC.Common.Threading;
using ASC.MessagingSystem;
using ASC.MessagingSystem.Data;
using ASC.Web.Studio.Wallet;

namespace ASC.Web.Studio.Extensions;

public static class WebStudioServiceExtensions
{
    public static IServiceCollection AddWebStudioServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddBaseDbContextPool<FilesDbContext>();
        services.RegisterQuotaFeature();
        services.AddHttpClient();
        services.AddHostedService<WorkerService>();
        services.TryAddSingleton(new ConcurrentQueue<WebhookRequestIntegrationEvent>());

        services.AddSingleton(Channel.CreateUnbounded<EventData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<EventData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<EventData>>().Writer);
        services.AddScoped<EventDataIntegrationEventHandler>();
        services.AddSingleton<MessageSenderService>();
        services.AddHostedService<MessageSenderService>();

        services.RegisterQueue<RemovePortalOperation>();
        services.RegisterQueue<MigrationOperation>(timeUntilUnregisterInSeconds: 60 * 60 * 24);

        services.AddActivePassiveHostedService<TopUpWalletService>(configuration);
        services.AddActivePassiveHostedService<RenewSubscriptionService>(configuration);

        services.AddWebhookSenderHttpClient(configuration);

        return services;
    }

    public static IApplicationBuilder UseWebStudioMiddleware(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("openApi:enable") &&
            configuration.GetValue<bool>("openApi:enableUI"))
        {
            var endpoints = new Dictionary<string, string>();
            configuration.Bind("openApi:endpoints", endpoints);
            app.UseOpenApiUI(endpoints);
        }

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("ssologin.ashx"),
            appBranch => appBranch.UseSsoHandler());

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("login.ashx"),
            appBranch => appBranch.UseLoginHandler());

        return app;
    }

    public static async Task SubscribeWebStudioEvents(this IEventBus eventBus)
    {
        await Task.WhenAll(
            eventBus.SubscribeAsync<RemovePortalIntegrationEvent,
                RemovePortalIntegrationEventHandler>(),
            eventBus.SubscribeAsync<MigrationParseIntegrationEvent,
                MigrationIntegrationEventHandler>(),
            eventBus.SubscribeAsync<MigrationIntegrationEvent,
                MigrationIntegrationEventHandler>(),
            eventBus.SubscribeAsync<MigrationCancelIntegrationEvent,
                MigrationIntegrationEventHandler>(),
            eventBus.SubscribeAsync<MigrationClearIntegrationEvent,
                MigrationIntegrationEventHandler>(),
            eventBus.SubscribeAsync<EventDataIntegrationEvent,
                EventDataIntegrationEventHandler>());
    }
}
