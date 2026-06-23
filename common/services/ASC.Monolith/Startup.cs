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

using ASC.ApiSystem.Extensions;
using ASC.Data.Backup.Extensions;
using ASC.Data.Backup.Worker.Extensions;
using ASC.Files.Extensions;
using ASC.Files.Worker.Extensions;
using ASC.Notify.Extensions;
using ASC.People.Extensions;
using ASC.Studio.Notify.Extensions;
using ASC.TelegramService.Extensions;
using ASC.Web.Api.Extensions;
using ASC.Web.Studio.Extensions;

namespace ASC.Monolith;

/// <summary>
/// Combined startup that merges all DocSpace .NET services into a single process.
/// Inherits BaseStartup for API services and delegates module registrations to shared extension methods.
/// </summary>
public class Startup : BaseStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
        if (configuration.GetSection("RabbitMQ").GetChildren().Any() &&
            string.IsNullOrEmpty(configuration["RabbitMQ:ClientProvidedName"]))
        {
            configuration["RabbitMQ:ClientProvidedName"] = "Monolith";
        }
    }

    public override async Task ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        services.AddMemoryCache();

        // === BaseStartup: core DI, auth, rate limiting, health checks, etc. ===
        await base.ConfigureServices(builder);

        // === Service registrations (shared with standalone services) ===
        services.AddWebApiServices(_configuration);
        services.AddWebStudioServices(_configuration);
        services.AddFilesServerServices(_configuration);
        services.AddFilesWorkerServices(_configuration);
        services.AddPeopleServices();
        services.AddAiServerServices(_configuration);
        services.AddAiWorkerServices(_configuration);
        services.AddBackupServices();
        services.AddBackupWorkerServices(_configuration);
        services.AddNotifyServices(_configuration);
        services.AddStudioNotifyServices(_configuration);
        services.AddTelegramServices(_configuration);

        // === ASC.ClearEvents ===
        services.AddHostedService<ClearEventsService>();

        // === ASC.ApiSystem authentication schemes ===
        services.AddApiSystemAuthServices();

        // === Controllers: add application parts from all API service assemblies ===
        services.AddControllers()
            .AddApplicationPart(typeof(ASC.Web.Api.Startup).Assembly)
            .AddApplicationPart(typeof(ASC.Web.Studio.Startup).Assembly)
            .AddApplicationPart(typeof(ASC.Files.Startup).Assembly)
            .AddApplicationPart(typeof(ASC.People.Startup).Assembly)
            .AddApplicationPart(typeof(ASC.AI.Startup).Assembly)
            .AddApplicationPart(typeof(ASC.Data.Backup.Startup).Assembly)
            .AddApplicationPart(typeof(ASC.Data.Backup.Worker.Startup).Assembly)
            .AddApplicationPart(typeof(ASC.TelegramService.Startup).Assembly)
            .AddApplicationPart(typeof(ASC.ApiSystem.Startup).Assembly);
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        base.Configure(app, env);

        app.UseWebApiMiddleware();
        app.UseWebStudioMiddleware(_configuration);
        app.UseFilesServerMiddleware();
        app.UseBackupMiddleware();

        // --- Endpoints ---
        app.UseEndpoints(endpoints =>
        {
            endpoints.InitializeHttpHandlers();
        });
    }

    /// <summary>
    /// Subscribe all event bus handlers from all services.
    /// Must be called after app.Build() since it requires IEventBus from DI.
    /// </summary>
    public static async Task SubscribeEventHandlers(IServiceProvider serviceProvider)
    {
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        await Task.WhenAll(
            eventBus.SubscribeFilesWorkerEvents(),
            eventBus.SubscribeWebStudioEvents(),
            eventBus.SubscribeBackupWorkerEvents(),
            eventBus.SubscribeNotifyEvents(),
            eventBus.SubscribeStudioNotifyEvents(),
            eventBus.SubscribeAiWorkerEvents());
    }
}
