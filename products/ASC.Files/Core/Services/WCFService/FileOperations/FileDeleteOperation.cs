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

using ASC.Files.Core.Services.WCFService.FileOperations;
using ASC.Webhooks.Core.EF.Model;

namespace ASC.Web.Files.Services.WCFService.FileOperations;

[ProtoContract]
public record FileDeleteOperationData<T> : FileOperationData<T>
{
    [ProtoMember(7)]
    public bool IgnoreException { get; set; }

    [ProtoMember(8)]
    public bool Immediately { get; set; }

    [ProtoMember(9)]
    public bool IsEmptyTrash { get; set; }

    [ProtoMember(10)]
    public IEnumerable<int> FilesVersions { get; set; }

    public FileDeleteOperationData()
    {

    }

    public FileDeleteOperationData(
        IEnumerable<T> folders,
        IEnumerable<T> files,
        IEnumerable<int> versions,
        int tenantId,
        Guid userId,
        IDictionary<string, string> headers,
        ExternalSessionSnapshot sessionSnapshot,
        bool holdResult = true,
        bool ignoreException = false,
        bool immediately = false,
        bool isEmptyTrash = false) : base(folders, files, tenantId, userId, headers, sessionSnapshot, holdResult)
    {
        IgnoreException = ignoreException;
        Immediately = immediately;
        IsEmptyTrash = isEmptyTrash;
        FilesVersions = versions;
    }
}

[Transient]
public class FileDeleteOperation : ComposeFileOperation<FileDeleteOperationData<string>, FileDeleteOperationData<int>>
{
    public FileDeleteOperation() { }

    public FileDeleteOperation(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override FileOperationType FileOperationType { get; set; } = FileOperationType.Delete;

    public override Task RunJob(CancellationToken cancellationToken)
    {
        DaoOperation = new FileDeleteOperation<int>(_serviceProvider, Data);
        ThirdPartyOperation = new FileDeleteOperation<string>(_serviceProvider, ThirdPartyData);

        return base.RunJob(cancellationToken);
    }
}

internal class FileDeleteOperation<T> : FileOperation<FileDeleteOperationData<T>, T>
{
    private const int DeleteBatchSize = 100;

    private int _trashId;
    private readonly bool _ignoreException;
    private readonly bool _immediately;
    private readonly bool _isEmptyTrash;
    private readonly Dictionary<string, StringValues> _headers;
    private readonly IEnumerable<int> _filesVersions;

    public override FileOperationType FileOperationType { get; set; } = FileOperationType.Delete;

    public FileDeleteOperation(IServiceProvider serviceProvider, FileDeleteOperationData<T> fileOperationData)
    : base(serviceProvider, fileOperationData)
    {
        _ignoreException = fileOperationData.IgnoreException;
        _immediately = fileOperationData.Immediately;
        _headers = fileOperationData.Headers?.ToDictionary(x => x.Key, x => new StringValues(x.Value));
        _isEmptyTrash = fileOperationData.IsEmptyTrash;
        _filesVersions = fileOperationData.FilesVersions;
    }

    protected override int InitTotalProgressSteps()
    {
        if (_filesVersions != null && _filesVersions.Any() && Files.Count > 0)
        {
            return _filesVersions.Count();
        }

        return base.InitTotalProgressSteps();
    }

    protected override async Task DoJob(AsyncServiceScope serviceScope)
    {
        var folderDao = serviceScope.ServiceProvider.GetService<IFolderDao<int>>();
        var filesMessageService = serviceScope.ServiceProvider.GetService<FilesMessageService>();
        var tenantManager = serviceScope.ServiceProvider.GetService<TenantManager>();

        await tenantManager.SetCurrentTenantAsync(CurrentTenantId);

        var externalShare = serviceScope.ServiceProvider.GetRequiredService<ExternalShare>();
        externalShare.Initialize(SessionSnapshot);
        _trashId = await folderDao.GetFolderIDTrashAsync(true);

        Folder<T> root = null;
        if (0 < Folders.Count)
        {
            root = await FolderDao.GetRootFolderAsync(Folders[0]);
        }
        else if (0 < Files.Count)
        {
            root = await FolderDao.GetRootFolderByFileAsync(Files[0]);
        }
        if (root != null)
        {
            Result += $"folder_{root.Id}{SplitChar}";
        }

        if (_filesVersions != null && _filesVersions.Any() && Files.Count > 0)
        {
            await DeleteFileVersionAsync(Files.FirstOrDefault(), _filesVersions, serviceScope);
        }
        else
        {
            if (_isEmptyTrash)
            {
                await DeleteFilesAsync(Files, serviceScope, true);
                await DeleteFoldersAsync(Folders, serviceScope, true);

                var trash = await folderDao.GetFolderAsync(_trashId);
                await filesMessageService.SendAsync(MessageAction.TrashEmptied, trash, _headers);
            }
            else
            {
                await DeleteFilesAsync(Files, serviceScope, true);
                await DeleteFoldersAsync(Folders, serviceScope, true);
            }
        }
    }

    private async Task DeleteFoldersAsync(IEnumerable<T> folderIds, IServiceScope scope, bool isNeedSendActions = false, bool checkPermissions = true)
    {
        var scopeClass = scope.ServiceProvider.GetService<FileDeleteOperationScope>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var webhookManager = scope.ServiceProvider.GetService<WebhookManager>();
        var fileSharing = scope.ServiceProvider.GetService<FileSharing>();
        var authContext = scope.ServiceProvider.GetService<AuthContext>();
        var notifyClient = scope.ServiceProvider.GetService<NotifyClient>();
        var permissionsManager = scope.ServiceProvider.GetService<DeletePermissionsCheck<T>>();
        var tenantQuotaFeatureStatHelper = scope.ServiceProvider.GetService<TenantQuotaFeatureStatHelper>();
        var quotaSocketManager = scope.ServiceProvider.GetService<QuotaSocketManager>();

        var (fileMarker, filesMessageService, roomLogoManager) = scopeClass;
        roomLogoManager.EnableAudit = false;

        var webhookTrigger = WebhookTrigger.All;
        IEnumerable<DbWebhooksConfig> webhookConfigs = null;

        foreach (var folderId in folderIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            // Intentional re-read: must use current folder state at execution time (TOCTOU mitigation).
            // Pre-check was done before enqueue, but permissions/locks/existence may have changed.
            var folder = await FolderDao.GetFolderAsync(folderId);
            var isRoom = folder.IsRoom;

            var canDelete = await FilesSecurity.CanDeleteAsync(folder);
            checkPermissions = isRoom ? !canDelete : checkPermissions;

            T canCalculate = default;

            var errorMsg = await permissionsManager.CheckFolderPermissionsAsync(
                [folder], _immediately, checkPermissions, !_ignoreException);
            if (errorMsg != null)
            {
                if (!_ignoreException && checkPermissions && !canDelete)
                {
                    canCalculate = FolderDao.CanCalculateSubitems(folderId) ? default : folderId;
                }

                Err = errorMsg;
            }
            else
            {
                canCalculate = FolderDao.CanCalculateSubitems(folderId) ? default : folderId;
                await fileMarker.RemoveMarkAsNewForAllAsync(folder);

                if (folder.ProviderEntry && (folder.Id.Equals(folder.RootId) || isRoom))
                {
                    if (ProviderDao != null)
                    {
                        List<AceWrapper> aces = null;

                        if (folder.RootFolderType is FolderType.VirtualRooms or FolderType.Archive)
                        {
                            var providerInfo = await ProviderDao.GetProviderInfoAsync(folder.ProviderId);
                            if (providerInfo.FolderId != null)
                            {
                                await roomLogoManager.DeleteAsync(providerInfo.FolderId, checkPermissions);
                            }

                            aces = await fileSharing.GetSharedInfoAsync(folder);
                        }

                        if (isNeedSendActions)
                        {
                            webhookTrigger = isRoom
                                ? folder.IsAgent
                                    ? WebhookTrigger.AgentDeleted
                                    : WebhookTrigger.RoomDeleted
                                : WebhookTrigger.FolderDeleted;
                            webhookConfigs = await webhookManager.GetWebhookConfigsAsync(webhookTrigger, folder);
                        }

                        await socketManager.DeleteFolder(folder, action: async () => await ProviderDao.RemoveProviderInfoAsync(folder.ProviderId));

                        if (isRoom)
                        {
                            await notifyClient.SendRoomRemovedAsync(folder, aces, authContext.CurrentAccount.ID);
                        }

                        if (isNeedSendActions)
                        {
                            var action = isRoom
                                ? folder.FolderType == FolderType.AiRoom
                                    ? MessageAction.AgentDeleted
                                    : MessageAction.RoomDeleted
                                : MessageAction.ThirdPartyDeleted;

                            await filesMessageService.SendAsync(action, folder, _headers, folder.Id.ToString(), folder.ProviderKey);
                            await webhookManager.PublishAsync(webhookTrigger, webhookConfigs, folder);

                            if (isRoom && folder.RootFolderType is FolderType.VirtualRooms or FolderType.Archive)
                            {
                                var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                                _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                            }
                        }
                    }

                    ProcessedFolder(folderId);
                }
                else
                {
                    var immediately = _immediately || !FolderDao.UseTrashForRemoveAsync(folder);
                    if (immediately && FolderDao.UseRecursiveOperation(folder.Id, default(T)))
                    {
                        // fast path: the whole subtree is checked and cleaned at once, folder rows are
                        // then removed by the single DeleteFolderAsync cascade below; per-folder recursion
                        // stays as a fallback for mixed/restricted subtrees
                        List<(Folder<T> Folder, IEnumerable<Guid> Users, IEnumerable<Guid> SharedUsers)> deletedSubtree = null;

                        if (folder.Id is int && (_immediately || folder.RootFolderType == FolderType.TRASH))
                        {
                            deletedSubtree = await TryDeleteSubtreeContentAsync(folder, scope, checkPermissions);
                        }

                        if (deletedSubtree == null)
                        {
                            var files = await FileDao.GetFilesAsync(folder.Id).ToListAsync();
                            await DeleteFilesAsync(files, scope, checkPermissions: checkPermissions);

                            var folders = await FolderDao.GetFoldersAsync(folder.Id).ToListAsync();
                            await DeleteFoldersAsync(folders.Select(f => f.Id).ToList(), scope, checkPermissions: checkPermissions);
                        }

                        if (deletedSubtree != null || await FolderDao.IsEmptyAsync(folder.Id))
                        {
                            var aces = new List<AceWrapper>();

                            if (isRoom)
                            {
                                await roomLogoManager.DeleteAsync(folder.Id, checkPermissions);
                                aces = await fileSharing.GetSharedInfoAsync(folder);
                            }

                            if (isNeedSendActions)
                            {
                                webhookTrigger = isRoom
                                    ? folder.IsAgent
                                        ? WebhookTrigger.AgentDeleted
                                        : WebhookTrigger.RoomDeleted
                                    : WebhookTrigger.FolderDeleted;
                                webhookConfigs = await webhookManager.GetWebhookConfigsAsync(webhookTrigger, folder);
                            }

                            // the counter of a form filling room is kept on the Forms root folder, not on the VirtualRooms root;
                            // the Forms root is resolved before the room is deleted so that the counter transfer
                            // performed on its lazy creation still sees the room
                            var counterFolderId = isRoom && folder.RootFolderType == FolderType.VirtualRooms && folder.FolderType == FolderType.FillingFormsRoom
                                ? await FolderDao.GetFolderIDFormsAsync(true)
                                : folder.ParentId;

                            await socketManager.DeleteFolder(folder, action: async () => await FolderDao.DeleteFolderAsync(folder.Id));

                            if (deletedSubtree != null)
                            {
                                // the cascade above has removed the subfolder rows —
                                // only now their events and progress may be reported
                                foreach (var (subfolder, users, sharedUsers) in deletedSubtree)
                                {
                                    await socketManager.DeleteFolder(subfolder, users, sharedUsers: sharedUsers);
                                    ProcessedFolder(subfolder.Id);
                                    await ProgressStep(FolderDao.CanCalculateSubitems(subfolder.Id) ? default : subfolder.Id);
                                }
                            }

                            if (isRoom && folder.RootFolderType == FolderType.VirtualRooms)
                            {
                                await FolderDao.ChangeTreeFolderSizeAsync(counterFolderId, -folder.Counter);
                            }

                            if (isNeedSendActions)
                            {
                                if (isRoom)
                                {
                                    await notifyClient.SendRoomRemovedAsync(folder, aces, authContext.CurrentAccount.ID);
                                    await filesMessageService.SendAsync(
                                        folder.FolderType == FolderType.AiRoom ? MessageAction.AgentDeleted : MessageAction.RoomDeleted,
                                        folder,
                                        _headers,
                                        folder.Title);
                                }
                                else
                                {
                                    await filesMessageService.SendAsync(MessageAction.FolderDeleted, folder, _headers, folder.Title);
                                }

                                await webhookManager.PublishAsync(webhookTrigger, webhookConfigs, folder);

                                if (isRoom && folder.RootFolderType is FolderType.VirtualRooms or FolderType.Archive)
                                {
                                    var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                                    _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                                }
                            }

                            ProcessedFolder(folderId);
                        }
                    }
                    else
                    {
                        var files = await FileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true).ToListAsync();

                        if (folder.FolderType is FolderType.FormFillingFolderInProgress or FolderType.FormFillingFolderDone)
                        {
                            await FolderDao.ChangeFolderTypeAsync(folder, FolderType.DEFAULT);
                            var tasks = files.Select(async file =>
                            {
                                await LinkDao.DeleteAllLinkAsync(file.Id);
                                await FileDao.SaveProperties(file.Id, null);
                            });

                            await Task.WhenAll(tasks);
                        }

                        if (folder.ParentRoomType == FolderType.VirtualDataRoom)
                        {
                            var tasks = files.Where(file => file.IsForm).Select(async file =>
                            {
                                await FileDao.SaveProperties(file.Id, null);
                                await FileDao.DeleteFormRolesAsync(file.Id);
                            });

                            await Task.WhenAll(tasks);
                        }

                        errorMsg = await permissionsManager.CheckFilePermissionsAsync(files, true, checkPermissions);
                        if (!_ignoreException && errorMsg != null)
                        {
                            Err = errorMsg;
                        }
                        else
                        {
                            if (immediately)
                            {
                                var aces = new List<AceWrapper>();

                                if (isRoom)
                                {
                                    var room = await roomLogoManager.DeleteAsync(folder.Id, checkPermissions);
                                    await socketManager.UpdateFolderAsync(room);
                                    aces = await fileSharing.GetSharedInfoAsync(folder);
                                }

                                if (isNeedSendActions)
                                {
                                    webhookTrigger = isRoom
                                        ? folder.IsAgent
                                            ? WebhookTrigger.AgentDeleted
                                            : WebhookTrigger.RoomDeleted
                                        : WebhookTrigger.FolderDeleted;
                                    webhookConfigs = await webhookManager.GetWebhookConfigsAsync(webhookTrigger, folder);
                                }

                                // the counter of a form filling room is kept on the Forms root folder, not on the VirtualRooms root;
                                // the Forms root is resolved before the room is deleted so that the counter transfer
                                // performed on its lazy creation still sees the room
                                var counterFolderId = isRoom && folder.RootFolderType == FolderType.VirtualRooms && folder.FolderType == FolderType.FillingFormsRoom
                                    ? await FolderDao.GetFolderIDFormsAsync(true)
                                    : folder.ParentId;

                                await socketManager.DeleteFolder(folder, action: async () => await FolderDao.DeleteFolderAsync(folder.Id));

                                if (isRoom && folder.RootFolderType == FolderType.VirtualRooms)
                                {
                                    await FolderDao.ChangeTreeFolderSizeAsync(counterFolderId, -folder.Counter);
                                }

                                if (isNeedSendActions)
                                {
                                    if (isRoom)
                                    {
                                        await notifyClient.SendRoomRemovedAsync(folder, aces, authContext.CurrentAccount.ID);
                                        await filesMessageService.SendAsync(
                                            folder.FolderType == FolderType.AiRoom ? MessageAction.AgentDeleted : MessageAction.RoomDeleted,
                                            folder,
                                            _headers,
                                            folder.Title);
                                    }
                                    else
                                    {
                                        await filesMessageService.SendAsync(MessageAction.FolderDeleted, folder, _headers, folder.Title);
                                    }

                                    await webhookManager.PublishAsync(webhookTrigger, webhookConfigs, folder);

                                    if (isRoom && folder.RootFolderType is FolderType.VirtualRooms or FolderType.Archive)
                                    {
                                        var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                                        _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                                    }
                                }
                            }
                            else
                            {
                                if (isNeedSendActions)
                                {
                                    webhookTrigger = WebhookTrigger.FolderTrashed;
                                    webhookConfigs = await webhookManager.GetWebhookConfigsAsync(webhookTrigger, folder);
                                }

                                await socketManager.DeleteFolder(folder, action: async () => await FolderDao.MoveFolderAsync(folder.Id, _trashId, CancellationToken));

                                if (isNeedSendActions)
                                {
                                    await filesMessageService.SendAsync(MessageAction.FolderMovedToTrash, folder, _headers, folder.Title);
                                    await webhookManager.PublishAsync(webhookTrigger, webhookConfigs, folder);

                                    if (isRoom && folder.RootFolderType is FolderType.VirtualRooms or FolderType.Archive)
                                    {
                                        var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                                        _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                                    }
                                }
                            }

                            ProcessedFolder(folderId);
                        }
                    }
                }
            }

            await ProgressStep(canCalculate);
        }
    }

    // Deletes the content of the folder subtree without per-folder recursion: bulk permission check,
    // batched deletion of all subtree files, then per-subfolder new-marker cleanup. The folder rows
    // themselves are removed by the DeleteFolderAsync cascade on the root, so the returned subfolders
    // (with their pre-resolved socket recipients) get their events and progress reported by the caller
    // only after that cascade succeeds.
    // Returns null when the subtree cannot be handled this way (restricted/hidden items, providers,
    // permission errors, files left behind) — the caller then falls back to the per-folder recursion.
    private async Task<List<(Folder<T> Folder, IEnumerable<Guid> Users, IEnumerable<Guid> SharedUsers)>> TryDeleteSubtreeContentAsync(Folder<T> folder, IServiceScope scope, bool checkPermissions)
    {
        var permissionsManager = scope.ServiceProvider.GetService<DeletePermissionsCheck<T>>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        var scopeClass = scope.ServiceProvider.GetService<FileDeleteOperationScope>();
        var (fileMarker, _, _) = scopeClass;

        var subfolders = await FolderDao.GetFoldersAsync(folder.Id, null, FilterType.FoldersOnly, false, Guid.Empty, string.Empty, withSubfolders: true).ToListAsync();

        if (subfolders.Count == 0)
        {
            // flat folder: the regular path already deletes its files in batches
            return null;
        }

        if (subfolders.Exists(f => f.ProviderEntry || f.IsRoom))
        {
            return null;
        }

        var subtreeFilesCount = await FileDao.GetFilesCountAsync(folder.Id, FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true);

        // the queries above hide entries the current user is restricted from, while the folder cascade
        // would still remove them; the unfiltered count detects such entries and rejects the fast path
        if (await FolderDao.GetItemsCountAsync(folder.Id) != subfolders.Count + subtreeFilesCount)
        {
            return null;
        }

        var errorMsg = await permissionsManager.CheckFolderPermissionsAsync(subfolders, _immediately, checkPermissions, !_ignoreException);
        if (errorMsg != null)
        {
            return null;
        }

        // files are deleted page by page instead of materializing the whole subtree;
        // always the first page — deletion shifts the remaining files to the front.
        // A page that fails to shrink means some files cannot be deleted — give up,
        // the final count check below rejects the fast path then
        var previousCount = subtreeFilesCount;

        while (true)
        {
            var page = await FileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true, count: DeleteBatchSize)
                .Select(f => f.Id)
                .ToListAsync();

            if (page.Count == 0)
            {
                break;
            }

            await DeleteFilesAsync(page, scope, checkPermissions: checkPermissions);

            var remaining = await FileDao.GetFilesCountAsync(folder.Id, FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true);
            if (remaining >= previousCount)
            {
                break;
            }

            previousCount = remaining;
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var newTags = await tagDao.GetTagsAsync([TagType.New], subfolders).ToListAsync();
        var subfoldersById = subfolders.ToDictionary(f => f.Id.ToString());

        foreach (var tagGroup in newTags.Where(t => t.EntryId != null).GroupBy(t => t.EntryId.ToString()))
        {
            if (!subfoldersById.TryGetValue(tagGroup.Key, out var subfolder))
            {
                continue;
            }

            foreach (var owner in tagGroup.Select(t => t.Owner).Distinct())
            {
                await fileMarker.RemoveMarkAsNewAsync(subfolder, owner);
            }
        }

        // socket recipients come from security records that the root cascade removes
        var result = new List<(Folder<T>, IEnumerable<Guid>, IEnumerable<Guid>)>(subfolders.Count);

        foreach (var subfolder in subfolders)
        {
            var (users, sharedUsers) = await socketManager.GetDeleteRecipientsAsync(subfolder);
            result.Add((subfolder, users, sharedUsers));
        }

        // every subtree file must be gone before the cascade removes the folder rows;
        // checked last to keep the race window with concurrent uploads minimal
        if (await FolderDao.GetItemsCountAsync(folder.Id) != subfolders.Count)
        {
            return null;
        }

        return result;
    }

    private async Task DeleteFilesAsync(IEnumerable<T> fileIds, IServiceScope scope, bool isNeedSendActions = false, bool checkPermissions = true)
    {
        var scopeClass = scope.ServiceProvider.GetService<FileDeleteOperationScope>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var webhookManager = scope.ServiceProvider.GetService<WebhookManager>();
        var security = scope.ServiceProvider.GetService<DeletePermissionsCheck<T>>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();

        var (fileMarker, filesMessageService, _) = scopeClass;

        var webhookTrigger = WebhookTrigger.All;
        IEnumerable<DbWebhooksConfig> webhookConfigs = null;

        var toDeleteImmediately = new List<File<T>>();

        var ids = fileIds as IReadOnlyCollection<T> ?? fileIds?.ToList() ?? [];
        if (ids.Count == 0)
        {
            return;
        }

        // Intentional re-read: must use current file state at execution time (TOCTOU mitigation).
        // Pre-check was done before enqueue, but permissions/locks/existence may have changed.
        // The re-read and the permission checks are batched: one query for the whole set instead
        // of several per file.
        var filesById = new Dictionary<T, File<T>>(ids.Count);
        await foreach (var loaded in FileDao.GetFilesAsync(ids))
        {
            filesById.TryAdd(loaded.Id, loaded);
        }

        var loadedFiles = filesById.Values.ToList();
        var errors = await security.CheckFilesPermissionsAsync(loadedFiles, checkPermissions);

        // New-markers: one tag query for the whole set; per-owner removal runs only for
        // files that actually have them and will actually be processed
        var processable = loadedFiles
            .Where(f => !errors.TryGetValue(f.Id, out var e) || (_ignoreException && e != FilesCommonResource.ErrorMessage_FileNotFound))
            .ToList();

        if (processable.Count > 0)
        {
            var tagDao = daoFactory.GetTagDao<T>();
            var newTags = await tagDao.GetTagsAsync([TagType.New], processable).ToListAsync();
            var processableById = processable.ToDictionary(f => f.Id.ToString());

            foreach (var tagGroup in newTags.Where(t => t.EntryId != null && t.EntryType == FileEntryType.File).GroupBy(t => t.EntryId.ToString()))
            {
                if (!processableById.TryGetValue(tagGroup.Key, out var taggedFile))
                {
                    continue;
                }

                foreach (var owner in tagGroup.Select(t => t.Owner).Distinct())
                {
                    await fileMarker.RemoveMarkAsNewAsync(taggedFile, owner);
                }
            }
        }

        foreach (var fileId in ids)
        {
            CancellationToken.ThrowIfCancellationRequested();

            filesById.TryGetValue(fileId, out var file);
            var errorMsg = file == null ? FilesCommonResource.ErrorMessage_FileNotFound : errors.GetValueOrDefault(file.Id);

            if (errorMsg == FilesCommonResource.ErrorMessage_FileNotFound)
            {
                Err = errorMsg;
            }
            else if (!_ignoreException && errorMsg != null)
            {
                Err = errorMsg;
            }
            else
            {
                if (!_immediately && FileDao.UseTrashForRemove(file))
                {
                    await LinkDao.DeleteAllLinkAsync(file.Id);
                    await FileDao.SaveProperties(file.Id, null);
                    if (file.IsForm)
                    {
                        await FileDao.DeleteFormRolesAsync(file.Id);
                    }

                    try
                    {
                        if (isNeedSendActions)
                        {
                            webhookTrigger = WebhookTrigger.FileTrashed;
                            webhookConfigs = await webhookManager.GetWebhookConfigsAsync(webhookTrigger, file);
                        }

                        await socketManager.DeleteFileAsync(file, action: async () => await FileDao.MoveFileAsync(file.Id, _trashId, file.RootFolderType == FolderType.USER));

                        if (file.Id is int trashedFormFileId)
                        {
                            var factoryIndexerForm = scope.ServiceProvider.GetService<FactoryIndexerForm>();
                            await factoryIndexerForm.DeleteAsync(r => r.Where(s => s.Id, trashedFormFileId));
                        }

                        if (isNeedSendActions)
                        {
                            await filesMessageService.SendAsync(MessageAction.FileMovedToTrash, file, _headers, file.Title);
                            await webhookManager.PublishAsync(webhookTrigger, webhookConfigs, file);
                        }

                        if (file.ThumbnailStatus == Thumbnail.Waiting)
                        {
                            file.ThumbnailStatus = Thumbnail.NotRequired;
                            await FileDao.SetThumbnailStatusAsync(file, Thumbnail.NotRequired);
                        }
                    }
                    catch (Exception ex)
                    {
                        Err = ex.Message;
                        Logger.ErrorWithException(ex);
                    }
                }
                else
                {
                    // links, properties, form roles and tag links are removed by the batched delete
                    toDeleteImmediately.Add(file);
                    continue;
                }

                ProcessedFile(fileId);
            }

            await ProgressStep(fileId: FolderDao.CanCalculateSubitems(fileId) ? default : fileId);
        }

        foreach (var chunk in toDeleteImmediately.Chunk(DeleteBatchSize))
        {
            await DeleteFileBatchAsync(chunk, scope, isNeedSendActions);
        }
    }

    private async Task DeleteFileBatchAsync(IReadOnlyCollection<File<T>> files, IServiceScope scope, bool isNeedSendActions)
    {
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var webhookManager = scope.ServiceProvider.GetService<WebhookManager>();
        var webhookPublisher = scope.ServiceProvider.GetService<IWebhookPublisher>();
        var filesMessageService = scope.ServiceProvider.GetService<FilesMessageService>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        var folderDao = scope.ServiceProvider.GetService<IFolderDao<int>>();

        var tagDao = daoFactory.GetTagDao<T>();
        var fromRoomFileIds = await tagDao.GetTagsAsync([TagType.FromRoom], files)
            .Where(t => t.EntryType == FileEntryType.File && t.EntryId != null)
            .Select(t => t.EntryId.ToString())
            .ToHashSetAsync();

        Guid GetQuotaOwner(File<T> file) =>
            fromRoomFileIds.Contains(file.Id.ToString()) ? ASC.Core.Configuration.Constants.CoreSystem.ID : file.GetFileQuotaOwner();

        var hasHeaders = _headers is { Count: > 0 };
        var configsByFile = new Dictionary<T, IEnumerable<DbWebhooksConfig>>();

        if ((hasHeaders && isNeedSendActions) || !hasHeaders)
        {
            // per-file configs re-read the entry, so skip the loop entirely when
            // the tenant has no webhooks for the trigger at all
            var anyConfigs = (await webhookPublisher.GetWebhookConfigsAsync<File<T>>(WebhookTrigger.FileDeleted, null, null)).Any();

            if (anyConfigs)
            {
                foreach (var file in files)
                {
                    configsByFile[file.Id] = await webhookManager.GetWebhookConfigsAsync(WebhookTrigger.FileDeleted, file);
                }
            }
        }

        // socket recipients are resolved from security records that the deletion below
        // removes, so they are captured up front (the old per-file path did the same by
        // wrapping the deletion into the socket call)
        var socketRecipients = new Dictionary<T, (IEnumerable<Guid> Users, IEnumerable<Guid> SharedUsers)>();

        foreach (var file in files)
        {
            socketRecipients[file.Id] = await socketManager.GetDeleteRecipientsAsync(file);
        }

        if (typeof(T) != typeof(int))
        {
            // third-party daos delete files one by one and do not touch these tables
            foreach (var file in files)
            {
                await LinkDao.DeleteAllLinkAsync(file.Id);
                await FileDao.SaveProperties(file.Id, null);
                if (file.IsForm)
                {
                    await FileDao.DeleteFormRolesAsync(file.Id);
                }
            }
        }

        var deleted = new List<File<T>>(files.Count);

        try
        {
            // events are reported only for files this batch actually deleted: ids removed
            // by a concurrent flow in the meantime must not produce duplicate notifications
            var actuallyDeleted = (await FileDao.DeleteFilesAsync(files.Select(f => KeyValuePair.Create(f.Id, GetQuotaOwner(f))))).ToHashSet();
            deleted.AddRange(files.Where(f => actuallyDeleted.Contains(f.Id)));
        }
        catch (Exception ex)
        {
            // the deletion transaction has failed and rolled back (post-commit follow-ups
            // are best-effort inside the dao and do not throw); retry one by one so a single
            // problematic file does not block the rest, and report only files that really failed
            Logger.ErrorWithException(ex);

            foreach (var file in files)
            {
                try
                {
                    await FileDao.DeleteFileAsync(file.Id, GetQuotaOwner(file));
                    deleted.Add(file);
                }
                catch (Exception e)
                {
                    Err = e.Message;
                    Logger.ErrorWithException(e);
                }
            }
        }

        long archiveSize = 0, trashSize = 0;

        foreach (var file in deleted)
        {
            var (users, sharedUsers) = socketRecipients[file.Id];
            await socketManager.DeleteFileAsync(file, users: users, sharedUsers: sharedUsers);

            switch (file.RootFolderType)
            {
                case FolderType.Archive:
                    archiveSize += file.ContentLength;
                    break;
                case FolderType.TRASH:
                    trashSize += file.ContentLength;
                    break;
            }

            if (hasHeaders)
            {
                if (isNeedSendActions)
                {
                    await filesMessageService.SendAsync(MessageAction.FileDeleted, file, _headers, file.Title);

                    if (configsByFile.TryGetValue(file.Id, out var configs))
                    {
                        await webhookManager.PublishAsync(WebhookTrigger.FileDeleted, configs, file);
                    }
                }
            }
            else
            {
                await filesMessageService.SendAsync(MessageAction.FileDeleted, file, MessageInitiator.AutoCleanUp, file.Title);

                if (configsByFile.TryGetValue(file.Id, out var configs))
                {
                    await webhookManager.PublishAsync(WebhookTrigger.FileDeleted, configs, file);
                }
            }
        }

        if (archiveSize != 0)
        {
            var archiveId = await folderDao.GetFolderIDArchive(false);
            await folderDao.ChangeTreeFolderSizeAsync(archiveId, -archiveSize);
        }

        if (trashSize != 0)
        {
            await folderDao.ChangeTreeFolderSizeAsync(_trashId, -trashSize);
        }

        foreach (var file in files)
        {
            ProcessedFile(file.Id);
            await ProgressStep(fileId: FolderDao.CanCalculateSubitems(file.Id) ? default : file.Id);
        }
    }

    private async Task DeleteFileVersionAsync(T fileId, IEnumerable<int> versions, AsyncServiceScope scope)
    {
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var webhookManager = scope.ServiceProvider.GetService<WebhookManager>();
        var filesMessageService = scope.ServiceProvider.GetService<FilesMessageService>();
        var permissionManager = scope.ServiceProvider.GetService<DeletePermissionsCheck<T>>();

        var file = await FileDao.GetFileAsync(fileId);

        var errorMsg = await permissionManager.CheckVersionPermissionsAsync(file);
        if ((errorMsg == FilesCommonResource.ErrorMessage_FileNotFound) || (errorMsg == FilesCommonResource.ErrorMessage_SecurityException))
        {
            Err = errorMsg;
        }
        else
        {
            foreach (var v in versions)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (file.Version == v)
                {
                    Err = FilesCommonResource.ErrorMessage_SecurityException_FileVersion;
                }
                else if (!_ignoreException && errorMsg != null)
                {
                    Err = errorMsg;
                }
                else
                {
                    try
                    {
                        await FileDao.DeleteFileVersionAsync(file, v);
                        await socketManager.UpdateFileAsync(file);

                        if (_headers is { Count: > 0 })
                        {
                            await filesMessageService.SendAsync(MessageAction.FileVersionRemoved, file, _headers, file.Title, v.ToString());
                            await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Err = ex.Message;
                        Logger.ErrorWithException(ex);
                    }

                    Process++;
                    Result += $"file_{fileId}{SplitChar}";
                }

                await ProgressStep();
            }
        }
    }
}

[Scope]
public record FileDeleteOperationScope(
    FileMarker FileMarker,
    FilesMessageService FilesMessageService,
    RoomLogoManager RoomLogoManager);
