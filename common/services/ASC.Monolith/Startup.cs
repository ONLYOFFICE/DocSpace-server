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

using ASC.AI.Extensions;
using ASC.AI.Worker.Extensions;
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

        // === Monolith-specific: Tool permission fallback (Redis or In-memory) ===
        if (!ASC.Api.Core.Extensions.ServiceCollectionExtension.IsRedisEnabled(_configuration))
        {
            services.AddSingleton<IToolPermissionRequester, InMemoryToolPermissionRequester>();
            services.AddSingleton<IToolPermissionProvider, InMemoryToolPermissionProvider>();
        }

        // === Service registrations (shared with standalone services) ===
        services.AddWebApiServices(_configuration);
        services.AddWebStudioServices(_configuration);
        services.AddFilesServerServices(_configuration);
        services.AddFilesWorkerServices(_configuration);
        services.AddPeopleServices();
        services.AddAiServerServices();
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
            endpoints.InitializeHttpHandlers("files_template");
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
