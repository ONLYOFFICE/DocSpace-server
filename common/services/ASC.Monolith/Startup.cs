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

namespace ASC.Monolith;

/// <summary>
/// Combined startup that merges all DocSpace .NET services into a single process.
/// Inherits BaseStartup for API services and manually registers worker services.
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

        // === In-memory fallbacks when Redis is disabled ===
        if (!ASC.Api.Core.Extensions.ServiceCollectionExtension.IsRedisEnabled(_configuration))
        {
            services.AddSingleton<IToolPermissionRequester, InMemoryToolPermissionRequester>();
            services.AddSingleton<IToolPermissionProvider, InMemoryToolPermissionProvider>();
        }

        // === DbContexts (union of all services) ===
        services.AddBaseDbContextPool<FilesDbContext>();
        services.AddBaseDbContextPool<BackupsContext>();
        services.AddBaseDbContextPool<NotifyDbContext>();
        services.AddBaseDbContextPool<AiDbContext>();

        // === Quota ===
        services.RegisterQuotaFeature();
        services.RegisterFreeBackupQuotaFeature();

        // === Kestrel limits (backup needs 1GB) ===
        var maxRequestLimit = 1024L * 1024L * 1024L;
        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = maxRequestLimit;
        });
        services.Configure<FormOptions>(x =>
        {
            x.MultipartBodyLengthLimit = maxRequestLimit;
        });

        // === Document service HTTP client (used by Web.Api, Files) ===
        services.AddDocumentServiceHttpClient(_configuration);

        // ======================================================================
        // ASC.Web.Api services
        // ======================================================================
        if (!_configuration.GetValue<bool>("disableLdapNotifyService"))
        {
            services.AddHostedService<LdapNotifyService>();
        }

        services.RegisterQueue<LdapOperationJob>();
        services.RegisterQueue<SmtpJob>();
        services.RegisterQueue<UsersQuotaSyncJob>();
        services.AddStartupTask<CspStartupTask>().TryAddSingleton(services);
        services.AddActivePassiveHostedService<NotifySchedulerService>(_configuration, "WebApiNotifySchedulerService");

        // ======================================================================
        // ASC.Web.Studio services
        // ======================================================================
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
        services.AddActivePassiveHostedService<TopUpWalletService>(_configuration);
        services.AddActivePassiveHostedService<RenewSubscriptionService>(_configuration);
        services.AddWebhookSenderHttpClient(_configuration);

        // ======================================================================
        // ASC.Files/Server services
        // ======================================================================
        services.AddScoped<IWebItem, ProductEntryPoint>();
        services.RegisterQueue<AsyncTaskData<int>>();
        services.RegisterQueue<AsyncTaskData<string>>();
        services.AddStartupTask<CheckPdfStartupTask>().TryAddSingleton(services);

        // ======================================================================
        // ASC.Files/Worker services (background jobs)
        // ======================================================================
        if (!Enum.TryParse<ElasticLaunchType>(_configuration["elastic:mode"], true, out var elasticLaunchType))
        {
            elasticLaunchType = ElasticLaunchType.Inclusive;
        }

        if (elasticLaunchType != ElasticLaunchType.Disabled)
        {
            services.AddHostedService<ElasticSearchIndexService>();
        }

        if (elasticLaunchType != ElasticLaunchType.Exclusive)
        {
            services.AddActivePassiveHostedService<FileConverterService<int>>(_configuration);
            services.AddActivePassiveHostedService<FileConverterService<string>>(_configuration);
            services.AddActivePassiveHostedService<PushNotificationService<int>>(_configuration);
            services.AddActivePassiveHostedService<PushNotificationService<string>>(_configuration);
            services.AddHostedService<ThumbnailBuilderService>();
            services.AddActivePassiveHostedService<AutoCleanTrashService>(_configuration);
            services.AddActivePassiveHostedService<AutoDeletePersonalFolderService>(_configuration);
            services.AddActivePassiveHostedService<AutoDeactivateExpiredApiKeysService>(_configuration);
            services.AddActivePassiveHostedService<DeleteExpiredService>(_configuration);
            services.AddActivePassiveHostedService<CleanupLifetimeExpiredService>(_configuration);
            services.AddActivePassiveHostedService<FrozenThumbnailProcessingService>(_configuration);
            services.AddSingleton(typeof(INotifyQueueManager<>), typeof(RoomNotifyQueueManager<>));

            if (_configuration["core:base-domain"] == "localhost" && !string.IsNullOrEmpty(_configuration["license:file:path"]))
            {
                services.AddActivePassiveHostedService<RefreshLicenseService>(_configuration);
            }
        }

        services.RegisterQueue<RoomIndexExportTask>();
        services.RegisterQueue<FileDeleteOperation>(10);
        services.RegisterQueue<FileMoveCopyOperation>(10);
        services.RegisterQueue<FileDuplicateOperation>(10);
        services.RegisterQueue<FileDownloadOperation>(10, timeUntilUnregisterInSeconds: 60 * 2);
        services.RegisterQueue<FileMarkAsReadOperation>(10);
        services.RegisterQueue<FormFillingReportTask>();
        services.RegisterQueue<CreateRoomTemplateOperation>();
        services.RegisterQueue<CreateRoomFromTemplateOperation>();
        services.RegisterQueue<EncryptionOperation>(timeUntilUnregisterInSeconds: 60 * 60 * 24);
        services.RegisterQueue<CustomerOperationsReportTask>();

        services.AddSingleton(Channel.CreateUnbounded<FileData<int>>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<FileData<int>>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<FileData<int>>>().Writer);

        // ======================================================================
        // ASC.People/Server services
        // ======================================================================
        services.RegisterQueue<RemoveProgressItem>();
        services.RegisterQueue<DeletePersonalFolderProgressItem>();
        services.RegisterQueue<UpdateUserTypeProgressItem>();
        services.RegisterQueue<ReassignProgressItem>();

        // ======================================================================
        // ASC.AI/Server + Worker services
        // ======================================================================
        services.RegisterQueue<VectorizationTask>();
        services.RegisterQueue<MessageExportTask>();
        services.RegisterQueue<ChatExportTask>();
        services.RegisterQueue<ChatDeletionTask>();
        services.AddActivePassiveHostedService<OrphanAttachmentCleanerService>(_configuration);
        services.AddActivePassiveHostedService<DeletedChatCleanerService>(_configuration);

        // ======================================================================
        // ASC.Data.Backup services
        // ======================================================================
        // (DbContexts and quota already registered above)

        // ======================================================================
        // ASC.Data.Backup.Worker services (background jobs)
        // ======================================================================
        services.RegisterQueue<BackupProgressItem>(5, 60 * 60 * 24);
        services.RegisterQueue<RestoreProgressItem>(5, 60 * 60 * 24);
        services.RegisterQueue<TransferProgressItem>(5, 60 * 60 * 24);
        services.AddHostedService<BackupListenerService>();
        services.AddHostedService<BackupCleanerTempFileService>();
        services.AddHostedService<BackupWorkerService>();
        services.AddActivePassiveHostedService<BackupCleanerService>(_configuration);
        services.AddActivePassiveHostedService<BackupSchedulerService>(_configuration);
        services.AddBackupSchedulerServiceResiliencePipeline();

        // ======================================================================
        // ASC.Notify services (background worker)
        // ======================================================================
        services.AddActivePassiveHostedService<ASC.Notify.Services.NotifySenderService>(_configuration);
        services.AddActivePassiveHostedService<NotifyCleanerService>(_configuration);
        services.AddScoped(_ => UrlEncoder.Default);

        // ======================================================================
        // ASC.Studio.Notify services (background worker)
        // ======================================================================
        services.AddHostedService<ServiceLauncher>();
        services.AddActivePassiveHostedService<NotifySchedulerService>(_configuration, "StudioNotifySchedulerService");

        // ======================================================================
        // ASC.TelegramService
        // ======================================================================
        services.AddActivePassiveHostedService<TelegramListenerService>(_configuration);

        // ======================================================================
        // ASC.ClearEvents
        // ======================================================================
        services.AddHostedService<ClearEventsService>();

        // ======================================================================
        // ASC.ApiSystem authentication schemes
        // ======================================================================
        services.AddScoped<AuthHandler>();
        services.AddScoped<ApiSystemAuthHandler>();
        services.AddScoped<ApiSystemBasicAuthHandler>();

        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, AuthHandler>("auth:allowskip:default", _ => { })
            .AddScheme<AuthenticationSchemeOptions, AuthHandler>("auth:allowskip:registerportal", _ => { })
            .AddScheme<AuthenticationSchemeOptions, ApiSystemAuthHandler>("auth:portal", _ => { })
            .AddScheme<AuthenticationSchemeOptions, ApiSystemBasicAuthHandler>("auth:portalbasic", _ => { });

        // ======================================================================
        // Controllers: add application parts from all API service assemblies
        // ======================================================================
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

        // --- ASC.Web.Api middleware ---
        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("logoUploader.ashx"),
            appBranch => appBranch.UseLogoUploader());

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("logo.ashx"),
            appBranch => appBranch.UseLogoHandler());

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("payment.ashx"),
            appBranch => appBranch.UseAccountHandler());

        app.MapWhen(
            context => context.Request.Path.ToString().StartsWith(UrlShortRewriter.BasePath),
            appBranch => appBranch.UseUrlShortRewriter());

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("migrationFileUpload.ashx"),
            appBranch => appBranch.UseMigrationFileUploadHandler());

        // --- ASC.Web.Studio middleware ---
        if (OpenApiEnabled && _configuration.GetValue<bool>("openApi:enableUI"))
        {
            var endpoints = new Dictionary<string, string>();
            _configuration.Bind("openApi:endpoints", endpoints);
            app.UseOpenApiUI(endpoints);
        }

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("ssologin.ashx"),
            appBranch => appBranch.UseSsoHandler());

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("login.ashx"),
            appBranch => appBranch.UseLoginHandler());

        // --- ASC.Files middleware ---
        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("filehandler.ashx", StringComparison.OrdinalIgnoreCase),
            appBranch => appBranch.UseFileHandler());

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("ChunkedUploader.ashx", StringComparison.OrdinalIgnoreCase),
            appBranch => appBranch.UseChunkedUploaderHandler());

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("DocuSignHandler.ashx", StringComparison.OrdinalIgnoreCase),
            appBranch => appBranch.UseDocuSignHandler());

        // --- ASC.Data.Backup middleware ---
        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("backupFileUpload.ashx"),
            appBranch => appBranch.UseBackupFileUploadHandler());

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

        // --- ASC.Files.Worker event handlers ---
        await eventBus.SubscribeAsync<ThumbnailRequestedIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.ThumbnailRequestedIntegrationEventHandler>();
        await eventBus.SubscribeAsync<RoomIndexExportIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.RoomIndexExportIntegrationEventHandler>();
        await eventBus.SubscribeAsync<DeleteIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.DeleteIntegrationEventHandler>();
        await eventBus.SubscribeAsync<MoveOrCopyIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.MoveOrCopyIntegrationEventHandler>();
        await eventBus.SubscribeAsync<DuplicateIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.DuplicateIntegrationEventHandler>();
        await eventBus.SubscribeAsync<BulkDownloadIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.BulkDownloadIntegrationEventHandler>();
        await eventBus.SubscribeAsync<MarkAsReadIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.MarkAsReadIntegrationEventHandler>();
        await eventBus.SubscribeAsync<EmptyTrashIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.EmptyTrashIntegrationEventHandler>();
        await eventBus.SubscribeAsync<FormFillingReportIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.FormFillingReportIntegrationEventHandler>();
        await eventBus.SubscribeAsync<RoomNotifyIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.RoomNotifyIntegrationEventHandler>();
        await eventBus.SubscribeAsync<CreateRoomTemplateIntegrationEvent,
            ASC.Files.Core.RoomTemplates.Events.RoomTemplatesIntegrationEventHandler>();
        await eventBus.SubscribeAsync<CreateRoomFromTemplateIntegrationEvent,
            ASC.Files.Core.RoomTemplates.Events.RoomTemplatesIntegrationEventHandler>();
        await eventBus.SubscribeAsync<DataStorageEncryptionIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.DataStorageEncryptionIntegrationEventHandler>();
        await eventBus.SubscribeAsync<CustomerOperationsReportIntegrationEvent,
            ASC.Files.Worker.IntegrationEvents.EventHandling.CustomerOperationsReportIntegrationEventHandler>();

        // --- ASC.Web.Studio event handlers ---
        await eventBus.SubscribeAsync<RemovePortalIntegrationEvent,
            ASC.Web.Studio.IntegrationEvents.RemovePortalIntegrationEventHandler>();
        await eventBus.SubscribeAsync<MigrationParseIntegrationEvent,
            ASC.Migration.Core.Core.MigrationIntegrationEventHandler>();
        await eventBus.SubscribeAsync<MigrationIntegrationEvent,
            ASC.Migration.Core.Core.MigrationIntegrationEventHandler>();
        await eventBus.SubscribeAsync<MigrationCancelIntegrationEvent,
            ASC.Migration.Core.Core.MigrationIntegrationEventHandler>();
        await eventBus.SubscribeAsync<MigrationClearIntegrationEvent,
            ASC.Migration.Core.Core.MigrationIntegrationEventHandler>();
        await eventBus.SubscribeAsync<EventDataIntegrationEvent, EventDataIntegrationEventHandler>();

        // --- ASC.Data.Backup.Worker event handlers ---
        await eventBus.SubscribeAsync<BackupRequestIntegrationEvent,
        ASC.Data.Backup.IntegrationEvents.EventHandling.BackupRequestedIntegrationEventHandler>();
        await eventBus.SubscribeAsync<BackupRestoreRequestIntegrationEvent,
        ASC.Data.Backup.IntegrationEvents.EventHandling.BackupRestoreRequestedIntegrationEventHandler>();
        await eventBus.SubscribeAsync<IntegrationEvent,
        ASC.Data.Backup.IntegrationEvents.EventHandling.BackupDeleteScheldureRequestedIntegrationEventHandler>();

        // --- ASC.Notify event handlers ---
        await eventBus.SubscribeAsync<NotifyInvokeSendMethodRequestedIntegrationEvent,
            ASC.Notify.IntegrationEvents.EventHandling.NotifyInvokeSendMethodRequestedIntegrationEventHandler>();
        await eventBus.SubscribeAsync<NotifySendMessageRequestedIntegrationEvent,
            ASC.Notify.IntegrationEvents.EventHandling.NotifySendMessageRequestedIntegrationEventHandler>();

        // --- ASC.Studio.Notify event handlers ---
        await eventBus.SubscribeAsync<NotifyItemIntegrationEvent,
            ASC.Web.Studio.IntegrationEvents.NotifyItemIntegrationEventHandler>();

        // --- ASC.AI.Worker event handlers ---
        await eventBus.SubscribeAsync<VectorizationIntegrationEvent,
            ASC.AI.Worker.Handlers.VectorizationIntegrationEventHandler>();
        await eventBus.SubscribeAsync<MessageExportIntegrationEvent,
            ASC.AI.Worker.Handlers.MessageExportIntegrationEventHandler>();
        await eventBus.SubscribeAsync<ChatExportIntegrationEvent,
            ASC.AI.Worker.Handlers.ChatExportIntegrationEventHandler>();
        await eventBus.SubscribeAsync<ChatDeletionIntegrationEvent,
            ASC.AI.Worker.Handlers.ChatDeletionIntegrationEventHandler>();
    }
}
