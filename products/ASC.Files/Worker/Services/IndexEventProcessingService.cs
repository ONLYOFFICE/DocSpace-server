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

namespace ASC.Files.Worker.Services;

[Singleton]
public class IndexEventProcessingService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<IndexEventProcessingService> logger,
    ChannelReader<IntegrationEvent> channelReader)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in channelReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessEventAsync(@event);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }

    private async Task ProcessEventAsync(IntegrationEvent @event)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();

        var tenant = await tenantManager.GetTenantAsync(@event.TenantId);
        if (tenant == null || tenant.Status != TenantStatus.Active)
        {
            return;
        }

        tenantManager.SetCurrentTenant(tenant);

        var factoryIndexer = scope.ServiceProvider.GetRequiredService<FactoryIndexer>();
        if (!await factoryIndexer.CheckStateAsync())
        {
            return;
        }

        switch (@event)
        {
            case FileIndexIntegrationEvent fileEvent:
                await ProcessAsync(scope.ServiceProvider, fileEvent);
                break;
            case FolderIndexIntegrationEvent folderEvent:
                await ProcessAsync(scope.ServiceProvider, folderEvent);
                break;
        }
    }

    private static async Task ProcessAsync(IServiceProvider serviceProvider, FileIndexIntegrationEvent @event)
    {
        var factoryIndexer = serviceProvider.GetRequiredService<FactoryIndexerFile>();

        if (@event.Action == FileIndexAction.Delete)
        {
            var factoryIndexerFormData = serviceProvider.GetRequiredService<FactoryIndexerForm>();
            var factoryIndexerFileMetadata = serviceProvider.GetRequiredService<FactoryIndexerFileMetadata>();

            await factoryIndexer.DeleteAsync(r => r.Where(a => a.Id, @event.FileId));
            await factoryIndexerFormData.DeleteAsync(r => r.Where(a => a.Id, @event.FileId));
            await factoryIndexerFileMetadata.DeleteAsync(r => r.Where(a => a.Id, @event.FileId));

            return;
        }

        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<FilesDbContext>>();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        var dbFile = @event.Version > 0
            ? await filesDbContext.DbFileByVersionAsync(@event.TenantId, @event.FileId, @event.Version)
            : await filesDbContext.DbFileAsync(@event.TenantId, @event.FileId);

        if (dbFile == null)
        {
            return;
        }

        switch (@event.Action)
        {
            case FileIndexAction.Index:
                dbFile.Folders = await filesDbContext.DbFolderTreesAsync(dbFile.ParentId).ToListAsync();
                await factoryIndexer.IndexAsync(dbFile);
                break;
            case FileIndexAction.UpdateInfo:
                await factoryIndexer.UpdateAsync(dbFile, true, r => r.Title, r => r.ModifiedBy, r => r.ModifiedOn);
                break;
            case FileIndexAction.UpdateFolders:
                dbFile.Folders = await filesDbContext.DbFolderTreesAsync(dbFile.ParentId).ToListAsync();
                await factoryIndexer.UpdateAsync(dbFile, UpdateAction.Replace, w => w.Folders);
                break;
        }
    }

    private static async Task ProcessAsync(IServiceProvider serviceProvider, FolderIndexIntegrationEvent @event)
    {
        var factoryIndexer = serviceProvider.GetRequiredService<FactoryIndexerFolder>();

        if (@event.Action == FolderIndexAction.Delete)
        {
            if (@event.FolderIds is { Count: > 0 })
            {
                var factoryIndexerFolderMetadata = serviceProvider.GetRequiredService<FactoryIndexerFolderMetadata>();

                await factoryIndexer.DeleteAsync(r => r.In(a => a.Id, @event.FolderIds.ToArray()));
                await factoryIndexerFolderMetadata.DeleteAsync(r => r.In(a => a.Id, @event.FolderIds.ToArray()));
            }

            return;
        }

        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<FilesDbContext>>();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        var dbFolder = await filesDbContext.FolderAsync(@event.TenantId, @event.FolderId);
        if (dbFolder != null)
        {
            await factoryIndexer.IndexAsync(dbFolder);
        }
    }
}
