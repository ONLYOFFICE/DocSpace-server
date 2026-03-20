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

using System.Text.Encodings.Web;

using ASC.Data.Storage.Encryption;
using ASC.Data.Storage.Encryption.IntegrationEvents.Events;
using ASC.Files.Core.RoomTemplates.Operations;
using ASC.Files.Core.Services.NotifyService;
using ASC.Files.Worker.IntegrationEvents.EventHandling;
using ASC.Files.Worker.Services;
using ASC.Web.Files.Configuration;

namespace ASC.Files.Worker.Extensions;

public static class FilesWorkerServiceExtensions
{
    public static IServiceCollection AddFilesWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient();

        if (!Enum.TryParse<ElasticLaunchType>(configuration["elastic:mode"], true, out var elasticLaunchType))
        {
            elasticLaunchType = ElasticLaunchType.Inclusive;
        }

        if (elasticLaunchType != ElasticLaunchType.Disabled)
        {
            services.AddHostedService<ElasticSearchIndexService>();
        }

        if (elasticLaunchType != ElasticLaunchType.Exclusive)
        {
            services.AddActivePassiveHostedService<FileConverterService<int>>(configuration);
            services.AddActivePassiveHostedService<FileConverterService<string>>(configuration);

            services.AddActivePassiveHostedService<PushNotificationService<int>>(configuration);
            services.AddActivePassiveHostedService<PushNotificationService<string>>(configuration);

            services.AddHostedService<ThumbnailBuilderService>();
            services.AddActivePassiveHostedService<AutoCleanTrashService>(configuration);
            services.AddActivePassiveHostedService<AutoDeletePersonalFolderService>(configuration);
            services.AddActivePassiveHostedService<AutoDeactivateExpiredApiKeysService>(configuration);
            services.AddActivePassiveHostedService<DeleteExpiredService>(configuration);
            services.AddActivePassiveHostedService<CleanupLifetimeExpiredService>(configuration);
            services.AddActivePassiveHostedService<FrozenThumbnailProcessingService>(configuration);

            services.AddSingleton(typeof(INotifyQueueManager<>), typeof(RoomNotifyQueueManager<>));

            if (configuration["core:base-domain"] == "localhost" && !string.IsNullOrEmpty(configuration["license:file:path"]))
            {
                services.AddActivePassiveHostedService<RefreshLicenseService>(configuration);
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
        services.RegisterQueue<AsyncTaskData<int>>();
        services.RegisterQueue<AsyncTaskData<string>>();

        services.RegisterQuotaFeature();
        services.AddBaseDbContextPool<FilesDbContext>();
        services.AddScoped<IWebItem, ProductEntryPoint>();

        services.AddSingleton(Channel.CreateBounded<FileData<int>>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        }));
        services.AddSingleton(svc => svc.GetRequiredService<Channel<FileData<int>>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<FileData<int>>>().Writer);
        services.AddDocumentServiceHttpClient(configuration);

        services.AddScoped(_ => UrlEncoder.Default);

        return services;
    }

    public static async Task SubscribeFilesWorkerEvents(this IEventBus eventBus)
    {
        await Task.WhenAll(
            eventBus.SubscribeAsync<ThumbnailRequestedIntegrationEvent,
                ThumbnailRequestedIntegrationEventHandler>(),
            eventBus.SubscribeAsync<RoomIndexExportIntegrationEvent,
                RoomIndexExportIntegrationEventHandler>(),
            eventBus.SubscribeAsync<DeleteIntegrationEvent,
                DeleteIntegrationEventHandler>(),
            eventBus.SubscribeAsync<MoveOrCopyIntegrationEvent,
                MoveOrCopyIntegrationEventHandler>(),
            eventBus.SubscribeAsync<DuplicateIntegrationEvent,
                DuplicateIntegrationEventHandler>(),
            eventBus.SubscribeAsync<BulkDownloadIntegrationEvent,
                BulkDownloadIntegrationEventHandler>(),
            eventBus.SubscribeAsync<MarkAsReadIntegrationEvent,
                MarkAsReadIntegrationEventHandler>(),
            eventBus.SubscribeAsync<EmptyTrashIntegrationEvent,
                EmptyTrashIntegrationEventHandler>(),
            eventBus.SubscribeAsync<FormFillingReportIntegrationEvent,
                FormFillingReportIntegrationEventHandler>(),
            eventBus.SubscribeAsync<ExternalDbFormSubmissionIntegrationEvent,
                ExternalDbFormSubmissionIntegrationEventHandler>(),
            eventBus.SubscribeAsync<RoomNotifyIntegrationEvent,
                RoomNotifyIntegrationEventHandler>(),
            eventBus.SubscribeAsync<CreateRoomTemplateIntegrationEvent,
                RoomTemplatesIntegrationEventHandler>(),
            eventBus.SubscribeAsync<CreateRoomFromTemplateIntegrationEvent,
                RoomTemplatesIntegrationEventHandler>(),
            eventBus.SubscribeAsync<DataStorageEncryptionIntegrationEvent,
                DataStorageEncryptionIntegrationEventHandler>(),
            eventBus.SubscribeAsync<CustomerOperationsReportIntegrationEvent,
                CustomerOperationsReportIntegrationEventHandler>());
    }
}
