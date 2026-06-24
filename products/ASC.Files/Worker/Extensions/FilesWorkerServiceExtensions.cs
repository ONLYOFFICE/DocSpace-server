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

        services.RegisterQueue<ExternalDbSyncTask>(10);
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
            eventBus.SubscribeAsync<BuiltinDbFormSubmissionIntegrationEvent,
                BuiltinDbFormSubmissionIntegrationEventHandler>(),
            eventBus.SubscribeAsync<ExternalDbRoomSyncIntegrationEvent,
                ExternalDbRoomSyncIntegrationEventHandler>(),
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
