// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Threading.Channels;

using ASC.Common.Threading;
using ASC.EventBus.Abstractions;
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
