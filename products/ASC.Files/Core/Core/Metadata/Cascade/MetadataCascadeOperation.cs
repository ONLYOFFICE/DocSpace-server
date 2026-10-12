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

    /// <summary>
    /// The longest a pass waits for the previous one over the same tree. Long enough for any realistic subtree,
    /// finite so a stuck predecessor surfaces as an error instead of a silently hanging queue.
    /// </summary>
    private static readonly TimeSpan _runLockTimeout = TimeSpan.FromHours(1);

    private Guid _userId;
    private int _processed;
    private int _total;
    private MetadataIndexHelper _metadataIndexHelper;
    private MetadataEntryNotifier _metadataEntryNotifier;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The (source folder, template) pairs the pass wrote links for, and the fields of the templates as the pass read
    /// them (their template and their type): what the reconciliation after the pass checks against the state the
    /// templates, the fields and the sources are in by then.
    /// </summary>
    private readonly HashSet<(int SourceFolderId, int TemplateId)> _appliedSources = [];
    private readonly Dictionary<int, int> _fieldTemplates = [];
    private readonly Dictionary<int, MetadataFieldType> _fieldTypes = [];

    public int TenantId { get; set; }
    public int FolderId { get; set; }
    public MetadataCascadeMode Mode { get; set; }

    /// <summary>
    /// The templates the operation propagates, sorted. Public so the queue keeps it: the worker tells two
    /// operations on the same folder apart by it.
    /// </summary>
    public int[] TemplateIds { get; set; } = [];

    /// <summary>
    /// How the pass treats the values the sub-entries already hold. Public for the same reason as <see cref="TemplateIds"/>:
    /// a request with another mode must not be folded into a running operation.
    /// </summary>
    public MetadataConflictResolveType Conflict { get; set; }

    /// <summary>
    /// Whether the pass holds the run lock and is about to read the folder's values. Public so the queue keeps it: the
    /// worker folds a repeated request into a queued pass only while this is false (see <see cref="MetadataCascadeWorker.StartAsync"/>),
    /// a pass past this point carries the values of the request that enqueued it.
    /// </summary>
    public bool Started { get; set; }

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
        TemplateIds = templateIds.Distinct().Order().ToArray();
        Conflict = conflict;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var securityContext = scope.ServiceProvider.GetService<SecurityContext>();
        var fileSecurity = scope.ServiceProvider.GetService<FileSecurity>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var distributedLockProvider = scope.ServiceProvider.GetService<IDistributedLockProvider>();
        var logger = scope.ServiceProvider.GetService<ILogger<MetadataCascadeOperation>>();
        _metadataIndexHelper = scope.ServiceProvider.GetService<MetadataIndexHelper>();
        _metadataEntryNotifier = scope.ServiceProvider.GetService<MetadataEntryNotifier>();

        try
        {
            await tenantManager.SetCurrentTenantAsync(TenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            var folderDao = daoFactory.GetFolderDao<int>();
            var metadataDao = daoFactory.GetMetadataDao<int>();

            var folder = await folderDao.GetFolderAsync(FolderId) ?? throw new ItemNotFoundException();

            if (!await fileSecurity.CanEditMetadataAsync(folder))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            // the passes over one tree run one after another: two operations over nested or overlapping subtrees
            // would otherwise both read "no link yet" for the same entry and both insert it, failing a whole batch.
            // The trees are disjoint (a room, the "My documents" of a user), so a pass in one does not wait for a
            // long pass in another, the way it did while the lock covered the whole tenant
            await using (await distributedLockProvider.TryAcquireFairLockAsync($"lock_metadata_cascade_run_{TenantId}_{await GetTreeIdAsync(folderDao, folder)}", _runLockTimeout))
            {
                // published before anything is read: a request arriving from now on gets a pass of its own
                Started = true;
                await PublishChanges();

                CancellationToken.ThrowIfCancellationRequested();

                // the wait for the lock can be long: the right to edit is checked again now that the pass is about to write
                if (!await fileSecurity.CanEditMetadataAsync(folder))
                {
                    throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
                }

                try
                {
                    if (Mode == MetadataCascadeMode.Stamp)
                    {
                        await StampAsync(metadataDao, folder);
                    }
                    else
                    {
                        await AssignAsync(metadataDao, folder);
                    }
                }
                finally
                {
                    // a failed or cancelled pass has written its batches all the same, so what they wrote for a template
                    // or a field that changed under the pass is settled whichever way the pass ended
                    await ReconcileAsync(metadataDao, logger);
                }

                await socketManager.UpdateFolderAsync(folder);
            }

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

    /// <summary>
    /// The tree the pass runs in: the room for a folder inside one (the room itself included), otherwise the root of
    /// the section the folder is in (the "My documents" of its owner).
    /// </summary>
    private static async Task<int> GetTreeIdAsync(IFolderDao<int> folderDao, Folder<int> folder)
    {
        var (roomId, _, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(folder);

        return roomId > 0 ? roomId : folder.RootId;
    }

    private async Task AssignAsync(IMetadataDao<int> metadataDao, Folder<int> folder)
    {
        var fieldTemplates = await GetFieldTemplatesAsync(metadataDao, TemplateIds);

        var folderValues = await metadataDao.GetValuesAsync(FolderId, FileEntryType.Folder)
            .Where(v => fieldTemplates.ContainsKey(v.FieldId) && !v.IsEmpty)
            .ToListAsync();

        _total = folder.FoldersCount + folder.FilesCount + 1;
        _processed = 0;

        var subfolderIds = await metadataDao.GetSubtreeFolderIdsAsync(FolderId).ToListAsync();

        // nested folders cascading the same template keep their own values (the nearest
        // ancestor wins): their subtrees are excluded from the pass for that template
        var nestedCascadeLinks = await metadataDao.GetCascadeLinksInSubtreeAsync(FolderId, TemplateIds);

        foreach (var (groupTemplateIds, groupSubfolderIds) in await SplitByNestedCascadesAsync(metadataDao, TemplateIds, subfolderIds, nestedCascadeLinks))
        {
            var groupValues = folderValues.Where(v => groupTemplateIds.Contains(fieldTemplates[v.FieldId])).ToList();

            await ApplyToSubtreeAsync(metadataDao, groupSubfolderIds, groupTemplateIds, FolderId, groupValues, Conflict);
        }
    }

    /// <summary>
    /// Splits the templates by the nested folders that cascade them inside the subtree. The nearest cascading folder
    /// wins, so the subtree of such a folder is left out of the pass for that template; templates sharing the same
    /// set of nested cascading folders are processed in a single pass.
    /// </summary>
    private static async Task<List<(int[] TemplateIds, List<int> SubfolderIds)>> SplitByNestedCascadesAsync(
        IMetadataDao<int> metadataDao,
        IReadOnlyCollection<int> templateIds,
        List<int> subfolderIds,
        IReadOnlyCollection<MetadataTemplateLink> nestedCascadeLinks)
    {
        if (nestedCascadeLinks.Count == 0)
        {
            return [(templateIds.ToArray(), subfolderIds)];
        }

        var nestedRootsByTemplate = nestedCascadeLinks
            .GroupBy(l => l.TemplateId)
            .ToDictionary(g => g.Key, g => g.Select(l => (int)l.EntryId).ToHashSet());

        var emptyRoots = new HashSet<int>();
        var result = new List<(int[] TemplateIds, List<int> SubfolderIds)>();

        foreach (var group in templateIds.GroupBy(t => nestedRootsByTemplate.GetValueOrDefault(t, emptyRoots), HashSet<int>.CreateSetComparer()))
        {
            var groupSubfolderIds = subfolderIds;

            if (group.Key.Count > 0)
            {
                var excluded = (await metadataDao.GetFolderIdsInSubtreesAsync(group.Key)).ToHashSet();
                groupSubfolderIds = subfolderIds.Where(id => !excluded.Contains(id)).ToList();
            }

            result.Add((group.ToArray(), groupSubfolderIds));
        }

        return result;
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

        // the moved folder is the nearest cascading ancestor of its whole subtree for the templates it cascades
        // itself, so the destination's cascade of those templates has nothing to stamp below it
        var ownCascadeTemplateIds = await metadataDao.GetLinksAsync(FolderId, FileEntryType.Folder)
            .Where(l => l.Cascade)
            .Select(l => l.TemplateId)
            .ToListAsync();

        foreach (var templateId in ownCascadeTemplateIds)
        {
            nearestSources.Remove(templateId);
        }

        if (nearestSources.Count == 0)
        {
            return;
        }

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

        // the same rule the assign pass follows: a nested folder cascading the template is the nearest source for its own
        // subtree, so that subtree keeps its values even when the destination cascades with Overwrite
        var nestedCascadeLinks = await metadataDao.GetCascadeLinksInSubtreeAsync(FolderId, nearestSources.Keys);

        // how the folder supplying a template treats the values the entries already hold: (template, source folder) -> mode
        var conflictBySource = cascadeLinks.ToDictionary(l => (l.TemplateId, (int)l.EntryId), l => l.CascadeConflict);

        foreach (var sourceGroup in nearestSources.GroupBy(s => (SourceFolderId: s.Value, Conflict: conflictBySource[(s.Key, s.Value)])))
        {
            var sourceTemplateIds = sourceGroup.Select(s => s.Key).ToList();

            foreach (var (groupTemplateIds, groupSubfolderIds) in await SplitByNestedCascadesAsync(metadataDao, sourceTemplateIds, subfolderIds, nestedCascadeLinks))
            {
                var groupValues = effectiveValues.Where(v => groupTemplateIds.Contains(fieldTemplates[v.FieldId])).ToList();

                // the moved folder itself was stamped inline during the move, its content is stamped here with the rule the
                // source folder cascades with: Skip keeps the entries' own values, Overwrite replaces them with the folder's
                await ApplyToSubtreeAsync(metadataDao, groupSubfolderIds, groupTemplateIds, sourceGroup.Key.SourceFolderId, groupValues, sourceGroup.Key.Conflict);
            }
        }
    }

    /// <summary>
    /// Writes the links and the values batch by batch. The templates, the fields and the values were read once, at the
    /// start of the pass, and the pass may wait for the run lock and then run for a long time: before every batch the
    /// source folder is asked which of the templates it still cascades, and the fields of the values are asked whether
    /// they still exist with the type the pass read, so a template deleted or un-cascaded in the meantime (the deletion
    /// drops the folder's link as well) and a field deleted or re-typed stop being written from the next batch on. The
    /// batch in flight at that moment is caught by <see cref="ReconcileAsync"/>.
    /// </summary>
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
            if (!await ApplyBatchAsync(metadataDao, batch, FileEntryType.Folder, templateIds, sourceFolderId, values, conflict))
            {
                return;
            }
        }

        var parentFolderIds = subfolderIds.Append(FolderId).ToList();

        foreach (var parentsBatch in parentFolderIds.Chunk(BatchSize))
        {
            var fileIds = await metadataDao.GetFileIdsByParentFoldersAsync(parentsBatch).ToListAsync();

            foreach (var batch in fileIds.Chunk(BatchSize))
            {
                if (!await ApplyBatchAsync(metadataDao, batch, FileEntryType.File, templateIds, sourceFolderId, values, conflict))
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Writes one batch for the templates the source folder still cascades. False when none is left, so the caller
    /// stops walking the subtree.
    /// </summary>
    private async Task<bool> ApplyBatchAsync(
        IMetadataDao<int> metadataDao,
        int[] batch,
        FileEntryType entryType,
        IReadOnlyCollection<int> templateIds,
        int sourceFolderId,
        IReadOnlyCollection<MetadataValue> values,
        MetadataConflictResolveType conflict)
    {
        CancellationToken.ThrowIfCancellationRequested();

        var activeTemplateIds = await metadataDao.GetLinksAsync(sourceFolderId, FileEntryType.Folder)
            .Where(l => l.Cascade && templateIds.Contains(l.TemplateId))
            .Select(l => l.TemplateId)
            .ToListAsync();

        if (activeTemplateIds.Count == 0)
        {
            return false;
        }

        var activeValues = values.Where(v => activeTemplateIds.Contains(_fieldTemplates[v.FieldId])).ToList();

        if (activeValues.Count > 0)
        {
            var liveFields = await metadataDao.GetFieldsAsync(activeValues.Select(v => v.FieldId).Distinct()).ToDictionaryAsync(f => f.Id);

            activeValues.RemoveAll(v => !liveFields.TryGetValue(v.FieldId, out var field) || !SameKind(field.Type, _fieldTypes[v.FieldId]));
        }

        foreach (var templateId in activeTemplateIds)
        {
            _appliedSources.Add((sourceFolderId, templateId));
        }

        var changed = await metadataDao.ApplyCascadeBatchAsync(batch, entryType, activeTemplateIds, sourceFolderId, activeValues, conflict);

        await _metadataIndexHelper.IndexEntriesAsync(entryType, batch);

        // the entries the pass changed are open in nobody's request: the clients viewing their folders re-read them
        await _metadataEntryNotifier.NotifyUpdatedAsync(entryType, changed);

        await ReportProgressAsync(batch.Length);

        return true;
    }

    /// <summary>
    /// Settles what the pass wrote for a template or a field that changed under it. A batch could be in flight when
    /// the template was deleted (no foreign key rejects its rows, and nothing else would ever remove them), when a field
    /// was deleted or re-typed (the deletion removed the values it saw, the re-typing was allowed because it saw none),
    /// or when the source folder stopped cascading the template (the un-cascade converted the inherited links it saw,
    /// not the ones written after it). Runs whichever way the pass ended; its own failure is logged, not thrown, so it
    /// never hides the error of the pass.
    /// </summary>
    private async Task ReconcileAsync(IMetadataDao<int> metadataDao, ILogger<MetadataCascadeOperation> logger)
    {
        try
        {
            foreach (var templateGroup in _appliedSources.GroupBy(s => s.TemplateId))
            {
                var templateId = templateGroup.Key;
                var fieldIds = _fieldTemplates.Where(f => f.Value == templateId).Select(f => f.Key).ToList();

                if (await metadataDao.GetTemplateAsync(templateId, withFields: false) == null)
                {
                    // the entries holding the late rows were not known to the deletion, which told the clients about its own
                    var orphanedLinks = await metadataDao.GetLinksByTemplateAsync(templateId);

                    await metadataDao.DeleteOrphanedTemplateRowsAsync(templateId, fieldIds);

                    await _metadataEntryNotifier.NotifyUpdatedAsync(orphanedLinks.Select(l => ((int)l.EntryId, l.EntryType)));

                    continue;
                }

                await ReconcileFieldsAsync(metadataDao, fieldIds);

                foreach (var sourceFolderId in templateGroup.Select(s => s.SourceFolderId))
                {
                    var stillCascades = await metadataDao.GetLinksAsync(sourceFolderId, FileEntryType.Folder)
                        .AnyAsync(l => l.Cascade && l.TemplateId == templateId);

                    if (!stillCascades)
                    {
                        await metadataDao.ConvertCascadeLinksToDirectAsync(sourceFolderId, templateId);
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.ErrorMetadataCascade(e);
        }
    }

    /// <summary>
    /// The values the pass wrote for a field that is gone or holds another type by now are removed: they carry the type
    /// the pass read, which the field no longer has, and the field's own deletion could not see them.
    /// </summary>
    private async Task ReconcileFieldsAsync(IMetadataDao<int> metadataDao, IReadOnlyCollection<int> fieldIds)
    {
        if (fieldIds.Count == 0)
        {
            return;
        }

        var liveFields = await metadataDao.GetFieldsAsync(fieldIds).ToDictionaryAsync(f => f.Id);

        foreach (var fieldId in fieldIds)
        {
            var writtenType = _fieldTypes[fieldId];

            if (liveFields.TryGetValue(fieldId, out var field) && SameKind(field.Type, writtenType))
            {
                continue;
            }

            var entries = await metadataDao.DeleteValuesWrittenAsAsync(fieldId, writtenType);

            await _metadataEntryNotifier.NotifyUpdatedAsync(entries.Select(v => ((int)v.EntryId, v.EntryType)));
        }
    }

    /// <summary>
    /// Whether a value written for one type is still a value of the other: the two choice types share the option rows,
    /// every other pair keeps its own column.
    /// </summary>
    private static bool SameKind(MetadataFieldType a, MetadataFieldType b)
    {
        return a == b || (a is MetadataFieldType.SingleChoice or MetadataFieldType.MultiChoice && b is MetadataFieldType.SingleChoice or MetadataFieldType.MultiChoice);
    }

    /// <summary>
    /// The fields of the templates as the pass reads them, kept for the whole pass: the batches filter their values by
    /// them and the reconciliation compares them with the fields as they are by the end.
    /// </summary>
    private async Task<Dictionary<int, int>> GetFieldTemplatesAsync(IMetadataDao<int> metadataDao, IEnumerable<int> templateIds)
    {
        var fieldTemplates = new Dictionary<int, int>();

        foreach (var templateId in templateIds)
        {
            await foreach (var field in metadataDao.GetFieldsAsync(templateId))
            {
                fieldTemplates[field.Id] = templateId;
                _fieldTemplates[field.Id] = templateId;
                _fieldTypes[field.Id] = field.Type;
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
