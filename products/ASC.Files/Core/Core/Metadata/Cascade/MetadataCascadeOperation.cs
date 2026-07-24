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

namespace ASC.Files.Core;

[Transient]
public class MetadataCascadeOperation : DistributedTaskProgress
{
    private const int BatchSize = 1000;

    private Guid _userId;
    private int[] _templateIds;
    private MetadataConflictResolveType _conflict;
    private bool _unassign;
    private int _processed;
    private int _total;
    private MetadataIndexHelper _metadataIndexHelper;
    private readonly IServiceProvider _serviceProvider;

    public int TenantId { get; set; }
    public int FolderId { get; set; }

    public MetadataCascadeOperation()
    {
    }

    public MetadataCascadeOperation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Init(int tenantId, Guid userId, int folderId, IEnumerable<int> templateIds, MetadataConflictResolveType conflict, bool unassign)
    {
        TenantId = tenantId;
        FolderId = folderId;
        _userId = userId;
        _templateIds = templateIds.Distinct().ToArray();
        _conflict = conflict;
        _unassign = unassign;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var securityContext = scope.ServiceProvider.GetService<SecurityContext>();
        var fileSecurity = scope.ServiceProvider.GetService<FileSecurity>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var logger = scope.ServiceProvider.GetService<ILogger<MetadataCascadeOperation>>();
        _metadataIndexHelper = scope.ServiceProvider.GetService<MetadataIndexHelper>();

        try
        {
            await tenantManager.SetCurrentTenantAsync(TenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            var folderDao = daoFactory.GetFolderDao<int>();
            var metadataDao = daoFactory.GetMetadataDao<int>();

            var folder = await folderDao.GetFolderAsync(FolderId) ?? throw new ItemNotFoundException();

            if (!await fileSecurity.CanEditAsync(folder))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (_unassign)
            {
                await UnassignAsync(metadataDao);
            }
            else
            {
                await AssignAsync(metadataDao, folder);
            }

            await socketManager.UpdateFolderAsync(folder);

            Percentage = 100;
            IsCompleted = true;
        }
        catch (Exception ex)
        {
            logger.ErrorMetadataCascade(ex);
            Exception = ex;
            IsCompleted = true;
        }
        finally
        {
            await PublishChanges();
        }
    }

    private async Task AssignAsync(IMetadataDao<int> metadataDao, Folder<int> folder)
    {
        var templateFieldIds = new HashSet<int>();

        foreach (var templateId in _templateIds)
        {
            await foreach (var field in metadataDao.GetFieldsAsync(templateId))
            {
                templateFieldIds.Add(field.Id);
            }
        }

        var folderValues = await metadataDao.GetValuesAsync(FolderId, FileEntryType.Folder)
            .Where(v => templateFieldIds.Contains(v.FieldId) && !v.IsEmpty)
            .ToListAsync();

        _total = folder.FoldersCount + folder.FilesCount + 1;
        _processed = 0;

        var subfolderIds = await metadataDao.GetSubtreeFolderIdsAsync(FolderId).ToListAsync();

        foreach (var batch in subfolderIds.Chunk(BatchSize))
        {
            await metadataDao.ApplyCascadeBatchAsync(batch, FileEntryType.Folder, _templateIds, FolderId, folderValues, _conflict);
            await _metadataIndexHelper.IndexEntriesAsync(FileEntryType.Folder, batch);
            await ReportProgressAsync(batch.Length);
        }

        var parentFolderIds = subfolderIds.Append(FolderId).ToList();

        foreach (var parentsBatch in parentFolderIds.Chunk(BatchSize))
        {
            var fileIds = await metadataDao.GetFileIdsByParentFoldersAsync(parentsBatch).ToListAsync();

            foreach (var batch in fileIds.Chunk(BatchSize))
            {
                await metadataDao.ApplyCascadeBatchAsync(batch, FileEntryType.File, _templateIds, FolderId, folderValues, _conflict);
                await _metadataIndexHelper.IndexEntriesAsync(FileEntryType.File, batch);
                await ReportProgressAsync(batch.Length);
            }
        }
    }

    private async Task UnassignAsync(IMetadataDao<int> metadataDao)
    {
        var templateIds = _templateIds.Length > 0 ? _templateIds : null;

        var links = new List<MetadataTemplateLink>();

        if (templateIds != null)
        {
            foreach (var templateId in templateIds)
            {
                links.AddRange(await metadataDao.GetLinksBySourceFolderAsync(FolderId, templateId));
            }
        }
        else
        {
            links = await metadataDao.GetLinksBySourceFolderAsync(FolderId);
        }

        _total = links.Count + 1;
        _processed = 0;

        foreach (var group in links.GroupBy(l => l.EntryType))
        {
            var groupTemplateIds = group.Select(l => l.TemplateId).Distinct().ToList();
            var entryIds = group.Select(l => (int)l.EntryId).Distinct().ToList();

            foreach (var batch in entryIds.Chunk(BatchSize))
            {
                await metadataDao.RemoveCascadeBatchAsync(batch, group.Key, FolderId, groupTemplateIds);
                await _metadataIndexHelper.IndexEntriesAsync(group.Key, batch);
                await ReportProgressAsync(batch.Length);
            }
        }
    }

    private async Task ReportProgressAsync(int count)
    {
        _processed += count;
        Percentage = _total > 0 ? Math.Min(99, 100.0 * _processed / _total) : 99;
        await PublishChanges();
    }
}

public static partial class MetadataCascadeOperationLogger
{
    [LoggerMessage(LogLevel.Error, "Error while cascading metadata")]
    public static partial void ErrorMetadataCascade(this ILogger<MetadataCascadeOperation> logger, Exception exception);
}
