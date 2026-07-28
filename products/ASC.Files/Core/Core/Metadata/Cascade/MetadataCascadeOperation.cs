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
    private int _processed;
    private int _total;
    private MetadataIndexHelper _metadataIndexHelper;
    private readonly IServiceProvider _serviceProvider;

    public int TenantId { get; set; }
    public int FolderId { get; set; }
    public MetadataCascadeMode Mode { get; set; }

    public MetadataCascadeOperation()
    {
    }

    public MetadataCascadeOperation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Init(int tenantId, Guid userId, int folderId, IEnumerable<int> templateIds, MetadataConflictResolveType conflict, MetadataCascadeMode mode)
    {
        TenantId = tenantId;
        FolderId = folderId;
        Mode = mode;
        _userId = userId;
        _templateIds = templateIds.Distinct().ToArray();
        _conflict = conflict;
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

            if (Mode == MetadataCascadeMode.Stamp)
            {
                await StampAsync(metadataDao, folder);
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
        var fieldTemplates = await GetFieldTemplatesAsync(metadataDao, _templateIds);

        var folderValues = await metadataDao.GetValuesAsync(FolderId, FileEntryType.Folder)
            .Where(v => fieldTemplates.ContainsKey(v.FieldId) && !v.IsEmpty)
            .ToListAsync();

        _total = folder.FoldersCount + folder.FilesCount + 1;
        _processed = 0;

        var subfolderIds = await metadataDao.GetSubtreeFolderIdsAsync(FolderId).ToListAsync();

        // nested folders cascading the same template keep their own values (the nearest
        // ancestor wins): their subtrees are excluded from the pass for that template
        var nestedCascadeLinks = await metadataDao.GetCascadeLinksInSubtreeAsync(FolderId, _templateIds);

        if (nestedCascadeLinks.Count == 0)
        {
            await ApplyToSubtreeAsync(metadataDao, subfolderIds, _templateIds, FolderId, folderValues, _conflict);
            return;
        }

        var nestedRootsByTemplate = nestedCascadeLinks
            .GroupBy(l => l.TemplateId)
            .ToDictionary(g => g.Key, g => g.Select(l => (int)l.EntryId).ToHashSet());

        var emptyRoots = new HashSet<int>();

        // templates sharing the same set of nested cascading folders are processed in a single pass
        foreach (var group in _templateIds.GroupBy(t => nestedRootsByTemplate.GetValueOrDefault(t, emptyRoots), HashSet<int>.CreateSetComparer()))
        {
            var groupTemplateIds = group.ToArray();
            var groupValues = folderValues.Where(v => groupTemplateIds.Contains(fieldTemplates[v.FieldId])).ToList();

            var groupSubfolderIds = subfolderIds;

            if (group.Key.Count > 0)
            {
                var excluded = (await metadataDao.GetFolderIdsInSubtreesAsync(group.Key)).ToHashSet();
                groupSubfolderIds = subfolderIds.Where(id => !excluded.Contains(id)).ToList();
            }

            await ApplyToSubtreeAsync(metadataDao, groupSubfolderIds, groupTemplateIds, FolderId, groupValues, _conflict);
        }
    }

    private async Task StampAsync(IMetadataDao<int> metadataDao, Folder<int> folder)
    {
        var levelByFolderId = await metadataDao.GetAncestorLevelsAsync(FolderId);

        if (levelByFolderId.Count == 0)
        {
            return;
        }

        var cascadeLinks = await metadataDao.GetCascadeLinksByFoldersAsync(levelByFolderId.Keys);

        if (cascadeLinks.Count == 0)
        {
            return;
        }

        var linkTuples = cascadeLinks.Select(l => (l.TemplateId, (int)l.EntryId)).ToList();

        var nearestSources = MetadataCascadeResolver.ResolveNearestSources(linkTuples, levelByFolderId);

        var fieldTemplates = await GetFieldTemplatesAsync(metadataDao, nearestSources.Keys);

        var sourceFolderIds = cascadeLinks.Select(l => (int)l.EntryId).Distinct().ToList();

        var sourceValues = await metadataDao.GetValuesAsync(sourceFolderIds, FileEntryType.Folder)
            .Where(v => fieldTemplates.ContainsKey(v.FieldId) && !v.IsEmpty)
            .ToListAsync();

        var fieldSources = MetadataCascadeResolver.ResolveFieldSources(
            sourceValues.Select(v => (v.FieldId, (int)v.EntryId)),
            linkTuples,
            fieldTemplates,
            levelByFolderId);

        // the effective inherited rows: for each field, the whole group from its resolved source folder
        var effectiveValues = sourceValues
            .Where(v => fieldSources.TryGetValue(v.FieldId, out var sourceId) && (int)v.EntryId == sourceId)
            .ToList();

        _total = folder.FoldersCount + folder.FilesCount + 1;
        _processed = 0;

        var subfolderIds = await metadataDao.GetSubtreeFolderIdsAsync(FolderId).ToListAsync();

        foreach (var group in nearestSources.GroupBy(s => s.Value))
        {
            var groupTemplateIds = group.Select(s => s.Key).ToArray();
            var groupValues = effectiveValues.Where(v => groupTemplateIds.Contains(fieldTemplates[v.FieldId])).ToList();

            // the moved folder itself was stamped inline during the move, its files were not;
            // stamping uses Skip so the entries' own values always survive
            await ApplyToSubtreeAsync(metadataDao, subfolderIds, groupTemplateIds, group.Key, groupValues, MetadataConflictResolveType.Skip);
        }
    }

    private async Task ApplyToSubtreeAsync(
        IMetadataDao<int> metadataDao,
        IReadOnlyCollection<int> subfolderIds,
        IReadOnlyCollection<int> templateIds,
        int sourceFolderId,
        IReadOnlyCollection<MetadataValue> values,
        MetadataConflictResolveType conflict)
    {
        foreach (var batch in subfolderIds.Chunk(BatchSize))
        {
            await metadataDao.ApplyCascadeBatchAsync(batch, FileEntryType.Folder, templateIds, sourceFolderId, values, conflict);
            await _metadataIndexHelper.IndexEntriesAsync(FileEntryType.Folder, batch);
            await ReportProgressAsync(batch.Length);
        }

        var parentFolderIds = subfolderIds.Append(FolderId).ToList();

        foreach (var parentsBatch in parentFolderIds.Chunk(BatchSize))
        {
            var fileIds = await metadataDao.GetFileIdsByParentFoldersAsync(parentsBatch).ToListAsync();

            foreach (var batch in fileIds.Chunk(BatchSize))
            {
                await metadataDao.ApplyCascadeBatchAsync(batch, FileEntryType.File, templateIds, sourceFolderId, values, conflict);
                await _metadataIndexHelper.IndexEntriesAsync(FileEntryType.File, batch);
                await ReportProgressAsync(batch.Length);
            }
        }
    }

    private static async Task<Dictionary<int, int>> GetFieldTemplatesAsync(IMetadataDao<int> metadataDao, IEnumerable<int> templateIds)
    {
        var fieldTemplates = new Dictionary<int, int>();

        foreach (var templateId in templateIds)
        {
            await foreach (var field in metadataDao.GetFieldsAsync(templateId))
            {
                fieldTemplates[field.Id] = templateId;
            }
        }

        return fieldTemplates;
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
