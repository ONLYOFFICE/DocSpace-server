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

namespace ASC.Web.Files.Services.WCFService.FileOperations;

[ProtoContract]
public record FileMoveCopyOperationData<T> : FileOperationData<T>
{
    public FileMoveCopyOperationData()
    {
    }

    public FileMoveCopyOperationData(IEnumerable<T> Folders,
        IEnumerable<T> Files,
        int TenantId,
        Guid UserId,
        JsonElement DestFolderId,
        bool Copy,
        FileConflictResolveType ResolveType,
        bool ToFillOut,
        bool HoldResult = true,
        IDictionary<string, string> Headers = null,
        ExternalSessionSnapshot SessionSnapshot = null) : base(Folders, Files, TenantId, UserId, Headers, SessionSnapshot, HoldResult)
    {
        this.DestFolderId = DestFolderId.ToString();
        this.Copy = Copy;
        this.ResolveType = ResolveType;
        this.ToFillOut = ToFillOut;
    }

    [ProtoMember(7)] public string DestFolderId { get; init; }

    [ProtoMember(8)] public bool Copy { get; init; }

    [ProtoMember(9)] public FileConflictResolveType ResolveType { get; init; }

    [ProtoMember(10)] public bool ToFillOut { get; init; }
}

[Transient]
public class FileMoveCopyOperation : ComposeFileOperation<FileMoveCopyOperationData<string>, FileMoveCopyOperationData<int>>
{
    public FileMoveCopyOperation() { }
    public FileMoveCopyOperation(IServiceProvider serviceProvider) : base(serviceProvider) { }
    public override FileOperationType FileOperationType { get; set; } = FileOperationType.Copy;

    public void Init(bool holdResult, bool copy)
    {
        base.Init(holdResult);

        if (!copy)
        {
            FileOperationType = (int)FileOperationType.Move;
        }
    }

    public override void Init(FileMoveCopyOperationData<int> data, FileMoveCopyOperationData<string> thirdPartyData, string taskId)
    {
        base.Init(data, thirdPartyData, taskId);
        var copy = data?.Copy ?? thirdPartyData?.Copy ?? false;

        if (!copy)
        {
            FileOperationType = (int)FileOperationType.Move;
        }
    }

    public override Task RunJob(CancellationToken cancellationToken)
    {
        DaoOperation = new FileMoveCopyOperation<int>(_serviceProvider, Data);
        ThirdPartyOperation = new FileMoveCopyOperation<string>(_serviceProvider, ThirdPartyData);

        return base.RunJob(cancellationToken);
    }
}

internal class FileMoveCopyOperation<T> : FileOperation<FileMoveCopyOperationData<T>, T>
{
    private readonly bool _copy;
    private readonly int _daoFolderId;
    private readonly IDictionary<string, StringValues> _headers;
    private readonly FileConflictResolveType _resolveType;
    private readonly string _thirdPartyFolderId;
    private readonly bool _toFillOut;

    public FileMoveCopyOperation(IServiceProvider serviceProvider, FileMoveCopyOperationData<T> data)
        : base(serviceProvider, data)
    {
        if (!DestFolderIdRouteHelper.TryGetIntId(data.DestFolderId, out var i, out var s))
        {
            _thirdPartyFolderId = s;
        }
        else
        {
            _daoFolderId = i;
        }

        _copy = data.Copy;
        _resolveType = data.ResolveType;
        _toFillOut = data.ToFillOut;

        _headers = data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value));
        FileOperationType = _copy ? FileOperationType.Copy : FileOperationType.Move;
    }

    public override FileOperationType FileOperationType { get; set; } = FileOperationType.Copy;

    protected override async Task DoJob(AsyncServiceScope serviceScope)
    {
        if (_daoFolderId != 0)
        {
            await DoAsync(serviceScope, _daoFolderId);
        }

        if (!string.IsNullOrEmpty(_thirdPartyFolderId))
        {
            await DoAsync(serviceScope, _thirdPartyFolderId);
        }
    }

    private async Task DoAsync<TTo>(AsyncServiceScope scope, TTo tto)
    {
        var fileMarker = scope.ServiceProvider.GetService<FileMarker>();
        var folderDao = scope.ServiceProvider.GetService<IFolderDao<TTo>>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var permissionsManager = scope.ServiceProvider.GetService<PermissionCheckStarter<T, TTo>>();

        //TODO: check on each iteration?
        var toFolder = await folderDao.GetFolderAsync(tto);
        var parentFolders = await folderDao.GetParentFoldersAsync(toFolder.Id).ToListAsync();

        Err = await permissionsManager.CheckGeneralPermissionsAsync(Files, Folders, toFolder, _copy);
        if (Err != null)
        {
            return;
        }

        Result += $"folder_{tto}{SplitChar}";

        var needToMark = new List<FileEntry>();

        var moveOrCopyFoldersTask = await MoveOrCopyFoldersAsync(scope, Folders, toFolder, _copy, parentFolders);
        var moveOrCopyFilesTask = await MoveOrCopyFilesAsync(scope, Files, toFolder, _copy, parentFolders);

        needToMark.AddRange(moveOrCopyFilesTask);

        foreach (var folder in moveOrCopyFoldersTask)
        {
            if (toFolder.FolderType != FolderType.Archive && !folder.IsRoom)
            {
                needToMark.AddRange(await GetFilesAsync(scope, folder));
            }

            await socketManager.CreateFolderAsync(folder);
        }

        var ntm = needToMark.Distinct();
        foreach (var n in ntm)
        {
            switch (n)
            {
                case FileEntry<T> entry1:
                    await fileMarker.MarkAsNewAsync(entry1);
                    break;
                case FileEntry<TTo> entry2:
                    await fileMarker.MarkAsNewAsync(entry2);
                    break;
            }
        }
    }

    private async Task<List<File<TTo>>> GetFilesAsync<TTo>(AsyncServiceScope scope, Folder<TTo> folder)
    {
        var fileDao = scope.ServiceProvider.GetService<IFileDao<TTo>>();

        var files = await fileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true).ToListAsync();

        return files;
    }

    private async Task<List<Folder<TTo>>> MoveOrCopyFoldersAsync<TTo>(AsyncServiceScope scope, List<T> folderIds, Folder<TTo> toFolder, bool copy, List<Folder<TTo>> toFolderParents, bool checkPermissions = true)
    {
        var needToMark = new List<Folder<TTo>>();

        if (folderIds.Count == 0)
        {
            return needToMark;
        }

        var scopeClass = scope.ServiceProvider.GetService<FileMoveCopyOperationScope>();
        var (filesMessageService, fileMarker, _, _, _, _) = scopeClass;
        var folderDao = scope.ServiceProvider.GetService<IFolderDao<TTo>>();
        var countRoomChecker = scope.ServiceProvider.GetRequiredService<CountRoomChecker>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var webhookManager = scope.ServiceProvider.GetService<WebhookManager>();
        var tenantQuotaFeatureStatHelper = scope.ServiceProvider.GetService<TenantQuotaFeatureStatHelper>();
        var quotaSocketManager = scope.ServiceProvider.GetService<QuotaSocketManager>();
        var distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var roomLogoManager = scope.ServiceProvider.GetRequiredService<RoomLogoManager>();
        var fileSecurity = scope.ServiceProvider.GetRequiredService<FileSecurity>();
        var notifyClient = scope.ServiceProvider.GetRequiredService<NotifyClient>();
        var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
        var permissionsManager = scope.ServiceProvider.GetService<PermissionCheckStarter<T, TTo>>();

        var toFolderId = toFolder.Id;
        var isToFolder = Equals(toFolderId, _daoFolderId);

        var sb = new StringBuilder();
        sb.Append(Result);
        foreach (var folderId in folderIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var folder = await FolderDao.GetFolderAsync(folderId);
            Err = await permissionsManager.CheckFoldersPermissionsAsync(folder, toFolder, _copy, _resolveType);

            if (Err == null)
            {
                if (!Equals(folder.ParentId ?? default, toFolderId) || _resolveType == FileConflictResolveType.Duplicate)
                {
                    var isRoom = folder.IsRoom;
                    var isThirdPartyRoom = isRoom && folder.ProviderEntry;
                    var parentFolderTask = FolderDao.GetFolderAsync(folder.ParentId);

                    // streamed: the subtree can be arbitrarily large and is only needed for this check
                    var errorMsg = await permissionsManager.CheckFilesSecurityPermissionsAsync(
                        FileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true), false);

                    try
                    {
                        //if destination folder contains folder with same name then merge folders
                        var conflictFolder = folder.RootFolderType == FolderType.Privacy || isRoom ||
                                             (!Equals(folder.ParentId ?? default, toFolderId) && _resolveType == FileConflictResolveType.Duplicate)
                            ? null
                            : await folderDao.GetFolderAsync(folder.Title, toFolderId);
                        Folder<TTo> newFolder;

                        if (copy || conflictFolder != null)
                        {
                            if (conflictFolder != null)
                            {
                                if (!conflictFolder.ProviderEntry && _resolveType == FileConflictResolveType.Duplicate)
                                {
                                    conflictFolder.Id = default;
                                    conflictFolder.Title = await folderDao.GetAvailableTitleAsync(conflictFolder.Title, conflictFolder.ParentId);
                                    conflictFolder.Id = await folderDao.SaveFolderAsync(conflictFolder);
                                }

                                newFolder = conflictFolder;

                                await filesMessageService.SendCopyMessageAsync(newFolder, await parentFolderTask, toFolder, toFolderParents, false, _headers, [newFolder.Title, toFolder.Title, toFolder.Id.ToString()]);

                                await webhookManager.PublishAsync(WebhookTrigger.FolderCopied, newFolder);

                                if (isToFolder)
                                {
                                    needToMark.Add(conflictFolder);
                                }
                            }
                            else
                            {
                                var isRoomCopying = isRoom && Equals(folder.ParentId ?? default, toFolderId);

                                IDistributedLockHandle roomsCountCheckLock = null;

                                try
                                {
                                    if (isRoomCopying)
                                    {
                                        roomsCountCheckLock = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetRoomsCountCheckKey(CurrentTenantId));
                                        await countRoomChecker.CheckAppend();
                                    }

                                    newFolder = await FolderDao.CopyFolderAsync(folder.Id, toFolderId, CancellationToken);
                                }
                                finally
                                {
                                    if (roomsCountCheckLock != null)
                                    {
                                        await roomsCountCheckLock.ReleaseAsync();
                                    }
                                }

                                if (isRoomCopying)
                                {
                                    if (await roomLogoManager.CopyAsync(folder, newFolder))
                                    {
                                        newFolder.SettingsHasLogo = true;
                                        await folderDao.SaveFolderAsync(newFolder);
                                    }

                                    if (newFolder.FolderType != FolderType.CustomRoom)
                                    {
                                        var primaryExternalLink = (await FilesSecurity.GetSharesAsync(folder)).FirstOrDefault(r => r.SubjectType == SubjectType.PrimaryExternalLink);
                                        if (primaryExternalLink != null)
                                        {
                                            FileShareOptions options = null;
                                            if (primaryExternalLink.Options != null)
                                            {
                                                options = new FileShareOptions { Title = primaryExternalLink.Options.Title, ExpirationDate = primaryExternalLink.Options.ExpirationDate, Internal = primaryExternalLink.Options.Internal };
                                            }

                                            await fileSecurity.ShareAsync(newFolder.Id, newFolder.FileEntryType, Guid.NewGuid(), primaryExternalLink.Share, primaryExternalLink.SubjectType, options);
                                        }
                                    }
                                }

                                if (isRoomCopying)
                                {
                                    await filesMessageService.SendAsync(MessageAction.RoomCopied, newFolder, _headers, newFolder.Title);
                                    await webhookManager.PublishAsync(WebhookTrigger.RoomCopied, newFolder);
                                    var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                                    _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                                }
                                else
                                {
                                    await filesMessageService.SendCopyMessageAsync(newFolder, await parentFolderTask, toFolder, toFolderParents, false, _headers, [newFolder.Title, toFolder.Title, toFolder.Id.ToString()]);
                                    await webhookManager.PublishAsync(WebhookTrigger.FolderCopied, newFolder);
                                }

                                if (isToFolder)
                                {
                                    needToMark.Add(newFolder);
                                }

                                if (ProcessedFolder(folderId))
                                {
                                    sb.Append($"folder_{newFolder.Id}{SplitChar}");
                                }
                            }

                            if (toFolder.ProviderId == folder.ProviderId // crossDao operation is always recursive
                                && FolderDao.UseRecursiveOperation(folder.Id, toFolderId))
                            {
                                var toNewFolderParents = await folderDao.GetParentFoldersAsync(newFolder.Id).ToListAsync();

                                List<T> foldersForCopyIds;
                                if (folder.FolderType == FolderType.FillingFormsRoom)
                                {
                                    var foldersForCopy = await FolderDao.GetFoldersAsync(folder.Id).ToListAsync();
                                    foldersForCopyIds = foldersForCopy.Where(f => !DocSpaceHelper.IsFormsFillingSystemFolder(f.FolderType)).Select(f => f.Id).ToList();
                                }
                                else
                                {
                                    foldersForCopyIds = await FolderDao.GetFoldersAsync(folder.Id).Select(f => f.Id).ToListAsync();
                                }

                                await MoveOrCopyFilesAsync(scope, await FileDao.GetFilesAsync(folder.Id).ToListAsync(), newFolder, copy, toNewFolderParents, checkPermissions);
                                await MoveOrCopyFoldersAsync(scope, foldersForCopyIds, newFolder, copy, toNewFolderParents, checkPermissions);

                                if (!copy)
                                {
                                    if (checkPermissions && !await FilesSecurity.CanMoveAsync(folder))
                                    {
                                        Err = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                                    }
                                    else if (await FolderDao.IsEmptyAsync(folder.Id))
                                    {
                                        await socketManager.DeleteFolder(folder, action: async () => await FolderDao.DeleteFolderAsync(folder.Id));
                                        if (ProcessedFolder(folderId))
                                        {
                                            sb.Append($"folder_{newFolder.Id}{SplitChar}");
                                        }
                                    }
                                }
                                else
                                {
                                    if (ProcessedFolder(folderId))
                                    {
                                        sb.Append($"folder_{newFolder.Id}{SplitChar}");
                                    }
                                }
                            }
                            else
                            {
                                if (conflictFolder != null)
                                {
                                    if (_resolveType == FileConflictResolveType.Overwrite)
                                    {
                                        if (ProcessedFolder(folderId))
                                        {
                                            sb.Append($"folder_{folderId}{SplitChar}");
                                        }

                                        continue;
                                    }

                                    TTo newFolderId;
                                    if (copy)
                                    {
                                        newFolder = await FolderDao.CopyFolderAsync(folder.Id, toFolderId, CancellationToken);
                                        newFolderId = newFolder.Id;

                                        await filesMessageService.SendCopyMessageAsync(newFolder, await parentFolderTask, toFolder, toFolderParents, true, _headers, [newFolder.Title, toFolder.Title, toFolder.Id.ToString()]);
                                        await webhookManager.PublishAsync(WebhookTrigger.FolderCopied, newFolder);

                                        if (isToFolder)
                                        {
                                            needToMark.Add(newFolder);
                                        }

                                        if (ProcessedFolder(folderId))
                                        {
                                            sb.Append($"folder_{newFolderId}{SplitChar}");
                                        }
                                    }
                                    else if (checkPermissions && !await FilesSecurity.CanMoveAsync(folder))
                                    {
                                        Err = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                                    }
                                    else if (errorMsg != null)
                                    {
                                        Err = errorMsg;
                                    }
                                    else
                                    {
                                        await fileMarker.RemoveMarkAsNewForAllAsync(folder);

                                        newFolderId = await FolderDao.MoveFolderAsync(folder.Id, toFolderId, CancellationToken);
                                        newFolder = await folderDao.GetFolderAsync(newFolderId);
                                        var parentFolder = await parentFolderTask;

                                        await filesMessageService.SendMoveMessageAsync(newFolder, parentFolder, toFolder, toFolderParents, true, _headers, [newFolder.Title, toFolder.Title, toFolder.Id.ToString()]);
                                        await webhookManager.PublishAsync(parentFolder.FolderType == FolderType.TRASH ? WebhookTrigger.FolderRestored : WebhookTrigger.FolderMoved, newFolder);

                                        if (isToFolder)
                                        {
                                            needToMark.Add(newFolder);
                                        }

                                        if (ProcessedFolder(folderId))
                                        {
                                            sb.Append($"folder_{newFolderId}{SplitChar}");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (checkPermissions && !await FilesSecurity.CanMoveAsync(folder))
                            {
                                Err = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                            }
                            else if (errorMsg != null)
                            {
                                Err = errorMsg;
                            }
                            else
                            {
                                await fileMarker.RemoveMarkAsNewForAllAsync(folder);

                                TTo newFolderId = default;

                                if (isThirdPartyRoom)
                                {
                                    await ProviderDao.UpdateRoomProviderInfoAsync(new ProviderData { Id = folder.ProviderId, RootFolderType = toFolder.FolderType });

                                    await socketManager.DeleteFolder(folder);

                                    if (folder.RootFolderType is FolderType.USER or FolderType.Privacy && toFolder.IsRoom)
                                    {
                                        var shares = await fileSecurity.GetPureSharesAsync(folder, ShareFilterType.UserOrGroup, null, null).ToListAsync();
                                        List<Guid> forRemove = [];
                                        foreach (var s in shares)
                                        {
                                            await fileSecurity.ShareAsync(folder.Id, folder.FileEntryType, s.Subject, FileShare.None);
                                            forRemove.Add(s.Subject);
                                        }

                                        await socketManager.RemoveFromSharedAsync(folder, forRemove);
                                    }

                                    folder.FolderIdDisplay = IdConverter.Convert<T>(toFolderId.ToString());
                                    folder.RootFolderType = toFolder.FolderType;

                                    await socketManager.CreateFolderAsync(folder);
                                }
                                else
                                {
                                    IDistributedLockHandle moveRoomLock = null;
                                    IDistributedLockHandle roomsCountCheckLock = null;

                                    try
                                    {
                                        if (isRoom)
                                        {
                                            moveRoomLock = await distributedLockProvider.TryAcquireFairLockAsync($"move_room_{CurrentTenantId}");

                                            if (toFolder.FolderType == FolderType.VirtualRooms)
                                            {
                                                roomsCountCheckLock = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetRoomsCountCheckKey(CurrentTenantId));

                                                await countRoomChecker.CheckAppend();

                                                await socketManager.DeleteFolder(folder, action: async () =>
                                                {
                                                    newFolderId = await FolderDao.MoveFolderAsync(folder.Id, toFolderId, CancellationToken);
                                                });

                                                var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                                                _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                                            }
                                            else if (toFolder.FolderType == FolderType.Archive)
                                            {
                                                var userIDs = (await fileSecurity.WhoCanReadAsync(folder, true)).ToList();

                                                if (folder.CreateBy != securityContext.CurrentAccount.ID)
                                                {
                                                    userIDs.Add(folder.CreateBy);
                                                }

                                                await socketManager.DeleteFolder(folder, action: async () =>
                                                {
                                                    newFolderId = await FolderDao.MoveFolderAsync(folder.Id, toFolderId, CancellationToken);
                                                });

                                                await notifyClient.SendRoomMovedArchiveAsync(folder, userIDs, securityContext.CurrentAccount.ID);
                                                var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                                                _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                                            }
                                        }
                                        else
                                        {
                                            await socketManager.DeleteFolder(folder, action: async () =>
                                            {
                                                newFolderId = await FolderDao.MoveFolderAsync(folder.Id, toFolderId, CancellationToken);
                                            });

                                            if (folder.RootFolderType is FolderType.USER or FolderType.Privacy && toFolder.IsRoom)
                                            {
                                                var shares = await fileSecurity.GetPureSharesAsync(folder, ShareFilterType.UserOrGroup, null, null).ToListAsync();
                                                List<Guid> forRemove = [];
                                                foreach (var s in shares)
                                                {
                                                    await fileSecurity.ShareAsync(folder.Id, folder.FileEntryType, s.Subject, FileShare.None);
                                                    forRemove.Add(s.Subject);
                                                }

                                                await socketManager.RemoveFromSharedAsync(folder, forRemove);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (moveRoomLock != null)
                                        {
                                            await moveRoomLock.ReleaseAsync();
                                        }

                                        if (roomsCountCheckLock != null)
                                        {
                                            await roomsCountCheckLock.ReleaseAsync();
                                        }
                                    }
                                }

                                if (isRoom)
                                {
                                    if (toFolder.FolderType == FolderType.Archive)
                                    {
                                        var pins = await TagDao.GetTagsAsync(Guid.Empty, [TagType.Pin], new List<FileEntry<T>> { folder }).ToListAsync();
                                        if (pins.Count > 0)
                                        {
                                            await TagDao.RemoveTagsAsync(folder, pins.Select(r=> r.Id).ToList());
                                        }

                                        if (!isThirdPartyRoom)
                                        {
                                            await FolderDao.DeleteLifetimeSettings(folder);
                                        }

                                        await filesMessageService.SendAsync(MessageAction.RoomArchived, folder, _headers, folder.Title);
                                        await webhookManager.PublishAsync(WebhookTrigger.RoomArchived, folder);
                                    }
                                    else
                                    {
                                        await filesMessageService.SendAsync(MessageAction.RoomUnarchived, folder, _headers, folder.Title);
                                        await webhookManager.PublishAsync(WebhookTrigger.RoomRestored, folder);
                                    }
                                }
                                else
                                {
                                    var parentFolder = await parentFolderTask;
                                    newFolder = await folderDao.GetFolderAsync(newFolderId);
                                    if (newFolder != null)
                                    {
                                        await filesMessageService.SendMoveMessageAsync(newFolder, parentFolder, toFolder, toFolderParents, true, _headers, [folder.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString()]);
                                        await webhookManager.PublishAsync(parentFolder.FolderType == FolderType.TRASH ? WebhookTrigger.FolderRestored : WebhookTrigger.FolderMoved, folder);
                                    }
                                }

                                if (isToFolder && !EqualityComparer<TTo>.Default.Equals(newFolderId, default))
                                {
                                    newFolder = await folderDao.GetFolderAsync(newFolderId);
                                    needToMark.Add(newFolder);
                                }

                                if (ProcessedFolder(folderId))
                                {
                                    var id = isThirdPartyRoom ? folder.Id.ToString() : newFolderId.ToString();
                                    sb.Append($"folder_{id}{SplitChar}");
                                }
                            }
                        }

                        Result = sb.ToString();
                    }
                    catch (Exception ex)
                    {
                        Err = ex.Message;

                        Logger.ErrorWithException(ex);
                    }
                }
            }

            await ProgressStep(FolderDao.CanCalculateSubitems(folderId) ? default : folderId);
        }

        return needToMark;
    }

    private async Task<List<FileEntry<TTo>>> MoveOrCopyFilesAsync<TTo>(AsyncServiceScope scope, List<T> fileIds, Folder<TTo> toFolder, bool copy, List<Folder<TTo>> toParentFolders, bool checkPermissions = true)
    {
        var needToMark = new List<FileEntry<TTo>>();

        if (fileIds.Count == 0)
        {
            return needToMark;
        }

        var scopeClass = scope.ServiceProvider.GetService<FileMoveCopyOperationScope>();
        var (filesMessageService, fileMarker, fileUtility, global, lockerManager, thumbnailSettings) = scopeClass;
        var linkDao = scope.ServiceProvider.GetService<ILinkDao<TTo>>();
        var fileDao = scope.ServiceProvider.GetService<IFileDao<TTo>>();
        var fileTracker = scope.ServiceProvider.GetService<FileTrackerHelper>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var webhookManager = scope.ServiceProvider.GetService<WebhookManager>();
        var globalStorage = scope.ServiceProvider.GetService<GlobalStore>();
        var fileStorageService = scope.ServiceProvider.GetService<FileStorageService>();
        var cachedFolderDao = scope.ServiceProvider.GetService<ICacheFolderDao<T>>();
        var fileSecurity = scope.ServiceProvider.GetService<FileSecurity>();
        var permissionsManager = scope.ServiceProvider.GetService<PermissionCheckStarter<T, TTo>>();

        var toFolderId = toFolder.Id;
        var sb = new StringBuilder();

        // files of one operation usually share a handful of parents
        var parentFoldersCache = new Dictionary<T, Folder<T>>();

        async Task<Folder<T>> GetParentFolderAsync(T parentId)
        {
            if (!parentFoldersCache.TryGetValue(parentId, out var parent))
            {
                parent = await FolderDao.GetFolderAsync(parentId);
                parentFoldersCache.Add(parentId, parent);
            }

            return parent;
        }

        var fileIdsToProcess = fileIds;

        // fast path: plain move inside the internal dao without conflicts, quota owner or
        // room changes; anything more complex falls back to the per-file loop below.
        // Duplicate resolve type skips conflict detection for files entirely, so it is
        // batched as well
        if (!copy && typeof(T) == typeof(int) && toFolder.Id is int &&
            !toFolder.ProviderEntry &&
            toFolder.RootFolderType is not (FolderType.TRASH or FolderType.Archive or FolderType.Privacy) &&
            toFolder.FolderType is not FolderType.Knowledge)
        {
            fileIdsToProcess = await MoveFileBatchAsync(scope, fileIds, toFolder, toParentFolders, needToMark, sb, GetParentFolderAsync, checkPermissions);
        }

        foreach (var fileId in fileIdsToProcess)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var file = await FileDao.GetFileAsync(fileId);

            Err = await permissionsManager.CheckFilesPermissionsAsync(file, toFolder, _copy, _resolveType);

            var errorMsg = await permissionsManager.CheckFilesSecurityPermissionsAsync([file], checkPermissions);

            if (Err == null)
            {
                if (toFolder.RootFolderType == FolderType.VirtualRooms)
                {
                    if (toParentFolders.Any(folder => folder.FolderType == FolderType.FillingFormsRoom) && !file.IsForm)
                    {
                        Err = _copy ? FilesCommonResource.ErrorMessage_UploadToFormRoom : FilesCommonResource.ErrorMessage_MoveToFormRoom;
                        continue;
                    }
                }

                var deleteLinks = file.RootFolderType == FolderType.USER &&
                                  toFolder.RootFolderType is FolderType.VirtualRooms or FolderType.Archive or FolderType.TRASH;

                var parentFolder = await GetParentFolderAsync(file.ParentId);
                try
                {
                    var conflict = _resolveType == FileConflictResolveType.Duplicate ||
                                   file.RootFolderType == FolderType.Privacy ||
                                   file.Encrypted ||
                                   toFolder.FolderType == FolderType.Knowledge
                        ? null
                        : await fileDao.GetFileAsync(toFolderId, file.Title);

                    if (conflict == null || conflict.Category != file.Category)
                    {
                        File<TTo> newFile = null;
                        if (copy)
                        {
                            try
                            {
                                newFile = await FileDao.CopyFileAsync(file.Id, toFolderId); //Stream copy will occur inside dao

                                await filesMessageService.SendCopyMessageAsync(newFile, parentFolder, toFolder, toParentFolders, false, _headers, [newFile.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString()]);
                                await webhookManager.PublishAsync(WebhookTrigger.FileCopied, newFile);

                                needToMark.Add(newFile);
                                if (newFile.IsForm && _toFillOut)
                                {
                                    var properties = await fileDao.GetProperties(newFile.Id) ?? new EntryProperties<TTo> { FormFilling = new FormFillingProperties<TTo>() };
                                    properties.CopyToFillOut = true;
                                    await fileDao.SaveProperties(newFile.Id, properties);
                                }

                                //await entryManager.MarkAsRecent(newFile);
                                await socketManager.CreateFileAsync(newFile);

                                if (ProcessedFile(fileId))
                                {
                                    sb.Append($"file_{newFile.Id}{SplitChar}");
                                }
                            }
                            catch
                            {
                                if (newFile != null)
                                {
                                    await fileDao.DeleteFileAsync(newFile.Id);
                                }

                                throw;
                            }
                        }
                        else
                        {
                            if (errorMsg != null)
                            {
                                Err = errorMsg;
                            }
                            else
                            {
                                await fileMarker.RemoveMarkAsNewForAllAsync(file);

                                if (file.RootFolderType is FolderType.USER or FolderType.Privacy && toFolder.IsRoom)
                                {
                                    var shares = await fileSecurity.GetPureSharesAsync(file, ShareFilterType.UserOrGroup, null, null).ToListAsync();
                                    List<Guid> forRemove = [];
                                    foreach (var s in shares)
                                    {
                                        await fileSecurity.ShareAsync(file.Id, file.FileEntryType, s.Subject, FileShare.None);
                                        forRemove.Add(s.Subject);
                                    }

                                    await socketManager.RemoveFromSharedAsync(file, forRemove);
                                }

                                TTo newFileId = default;
                                await socketManager.DeleteFileAsync(file, action: async () =>
                                {
                                    newFileId = await FileDao.MoveFileAsync(file.Id, toFolderId, deleteLinks);
                                });

                                newFile = await fileDao.GetFileAsync(newFileId);

                                await filesMessageService.SendMoveMessageAsync(newFile, parentFolder, toFolder, toParentFolders, false, _headers, [file.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString()]);
                                await webhookManager.PublishAsync(parentFolder.FolderType == FolderType.TRASH ? WebhookTrigger.FileRestored : WebhookTrigger.FileMoved, newFile);

                                // if (newFile.RootFolderType != FolderType.TRASH)
                                // {
                                //     await entryManager.MarkAsRecent(newFile);
                                // }

                                if (file.RootFolderType == FolderType.TRASH && newFile.ThumbnailStatus == Thumbnail.NotRequired)
                                {
                                    newFile.ThumbnailStatus = Thumbnail.Waiting;

                                    await fileDao.SetThumbnailStatusAsync(newFile, Thumbnail.Waiting);
                                }

                                if (newFile.ProviderEntry)
                                {
                                    await LinkDao.DeleteAllLinkAsync(file.Id);
                                }

                                if (Equals(toFolderId, _daoFolderId))
                                {
                                    if (file.RootFolderType == FolderType.VirtualRooms &&
                                        toFolder.RootFolderType == FolderType.VirtualRooms &&
                                        !file.ProviderEntry)
                                    {
                                        var fromParents = await cachedFolderDao.GetParentFoldersAsync(file.ParentId).ToListAsync();
                                        var fromRoom = fromParents.FirstOrDefault(x => x.IsRoom);
                                        var toRoom = toParentFolders.FirstOrDefault(x => x.IsRoom);

                                        if (!fromRoom.Id.Equals((T)Convert.ChangeType(toRoom.Id, typeof(T))))
                                        {
                                            needToMark.Add(newFile);
                                        }
                                    }
                                    else
                                    {
                                        needToMark.Add(newFile);
                                    }
                                }

                                if (newFile.IsForm && _toFillOut)
                                {
                                    var properties = await fileDao.GetProperties(newFile.Id) ?? new EntryProperties<TTo> { FormFilling = new FormFillingProperties<TTo>() };
                                    properties.CopyToFillOut = true;
                                    await fileDao.SaveProperties(newFile.Id, properties);
                                }

                                await socketManager.CreateFileAsync(newFile);

                                if (file.IsForm)
                                {
                                    var toRoom = toParentFolders.FirstOrDefault(folder => folder.FolderType is FolderType.FillingFormsRoom or FolderType.VirtualDataRoom);
                                    var fromRoom = await DocSpaceHelper.GetParentRoom(file, FolderDao);
                                    if (fromRoom?.FolderType is FolderType.FillingFormsRoom or FolderType.VirtualDataRoom && (toRoom == null || !toRoom.Id.Equals(fromRoom.Id)))
                                    {
                                        var tasks = new List<Task> { FileDao.SaveProperties(file.Id, null) };

                                        if (fromRoom.FolderType is FolderType.FillingFormsRoom)
                                        {
                                            tasks.Add(LinkDao.DeleteAllLinkAsync(file.Id));
                                        }
                                        else if (fromRoom.FolderType is FolderType.VirtualDataRoom)
                                        {
                                            tasks.Add(FileDao.DeleteFormRolesAsync(file.Id));
                                        }

                                        await Task.WhenAll(tasks);
                                    }

                                    if (toRoom?.FolderType == FolderType.FillingFormsRoom)
                                    {
                                        var numberRoomMembers = await fileStorageService.GetPureSharesCountAsync(toFolder.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, "");
                                        var properties = await fileDao.GetProperties(newFile.Id) ?? new EntryProperties<TTo> { FormFilling = new FormFillingProperties<TTo>() };
                                        properties.FormFilling.StartFilling = true;
                                        properties.FormFilling.OriginalFormId = newFile.Id;

                                        await Task.WhenAll(
                                            fileDao.SaveProperties(newFile.Id, properties)
                                        );
                                    }
                                }

                                if (ProcessedFile(fileId))
                                {
                                    sb.Append($"file_{newFileId}{SplitChar}");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_resolveType == FileConflictResolveType.Overwrite)
                        {
                            if (checkPermissions && !await FilesSecurity.CanEditAsync(conflict) && !await FilesSecurity.CanFillFormsAsync(conflict))
                            {
                                Err = FilesCommonResource.ErrorMessage_SecurityException;
                            }
                            else if (await lockerManager.FileLockedForMeAsync(conflict.Id))
                            {
                                Err = FilesCommonResource.ErrorMessage_LockedFile;
                            }
                            else if (await fileTracker.IsEditingAsync(conflict.Id, false))
                            {
                                Err = FilesCommonResource.ErrorMessage_SecurityException_UpdateEditingFile;
                            }
                            else
                            {
                                var newFile = conflict;
                                newFile.Version++;
                                newFile.VersionGroup++;
                                newFile.PureTitle = file.PureTitle;
                                newFile.ConvertedType = file.ConvertedType;
                                newFile.Comment = FilesCommonResource.CommentOverwrite;
                                newFile.Encrypted = file.Encrypted;
                                newFile.ThumbnailStatus = file.ThumbnailStatus == Thumbnail.Created && !file.ProviderEntry ? Thumbnail.Creating : Thumbnail.Waiting;


                                await using (var stream = await FileDao.GetFileStreamAsync(file))
                                {
                                    newFile.ContentLength = stream.CanSeek ? stream.Length : file.ContentLength;

                                    newFile = await fileDao.SaveFileAsync(newFile, stream);
                                }

                                if (file.ThumbnailStatus == Thumbnail.Created && !file.ProviderEntry)
                                {
                                    var store = await globalStorage.GetStoreAsync();
                                    var thumbnailStatus = Thumbnail.Created;

                                    foreach (var size in thumbnailSettings.Sizes)
                                    {
                                        try
                                        {
                                            var path = FileDao.GetUniqThumbnailPath(file, size.Width, size.Height);
                                            var newPath = fileDao.GetUniqThumbnailPath(newFile, size.Width, size.Height);

                                            await store.CopyAsync(string.Empty, path, string.Empty, newPath);
                                        }
                                        catch (Exception)
                                        {
                                            thumbnailStatus = Thumbnail.Waiting;
                                            break;
                                        }
                                    }

                                    await fileDao.SetThumbnailStatusAsync(newFile, thumbnailStatus);

                                    newFile.ThumbnailStatus = thumbnailStatus;
                                }

                                await linkDao.DeleteAllLinkAsync(newFile.Id);

                                needToMark.Add(newFile);
                                if (newFile.IsForm && _toFillOut)
                                {
                                    var properties = await fileDao.GetProperties(newFile.Id) ?? new EntryProperties<TTo> { FormFilling = new FormFillingProperties<TTo>() };
                                    properties.CopyToFillOut = true;
                                    await fileDao.SaveProperties(newFile.Id, properties);
                                }

                                await socketManager.CreateFileAsync(newFile);
                                //await entryManager.MarkAsRecent(newFile);

                                if (copy)
                                {
                                    await filesMessageService.SendCopyMessageAsync(newFile, parentFolder, toFolder, toParentFolders.ToList(), true, _headers, [newFile.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString()]);
                                    await webhookManager.PublishAsync(WebhookTrigger.FileCopied, newFile);
                                    if (ProcessedFile(fileId))
                                    {
                                        sb.Append($"file_{newFile.Id}{SplitChar}");
                                    }
                                }
                                else
                                {
                                    if (Equals(file.ParentId.ToString(), toFolderId.ToString()))
                                    {
                                        if (ProcessedFile(fileId))
                                        {
                                            sb.Append($"file_{newFile.Id}{SplitChar}");
                                        }
                                    }
                                    else
                                    {
                                        if (errorMsg != null)
                                        {
                                            Err = errorMsg;
                                        }
                                        else
                                        {
                                            await socketManager.DeleteFileAsync(file, action: async () =>
                                            {
                                                await FileDao.DeleteFileAsync(file.Id);

                                                await LinkDao.DeleteAllLinkAsync(file.Id);
                                            });

                                            await filesMessageService.SendMoveMessageAsync(newFile, parentFolder, toFolder, toParentFolders, true, _headers, [file.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString()]);
                                            await webhookManager.PublishAsync(parentFolder.FolderType == FolderType.TRASH ? WebhookTrigger.FileRestored : WebhookTrigger.FileMoved, newFile);

                                            if (ProcessedFile(fileId))
                                            {
                                                sb.Append($"file_{newFile.Id}{SplitChar}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (_resolveType == FileConflictResolveType.Skip)
                        {
                            //nothing
                        }
                    }
                }
                catch (TenantQuotaException ex)
                {
                    Err = ex.Message;

                    Logger.InformationUnableFileMoveCopyOperation(fileId.ToString(), ex.Message);
                }
                catch (Exception ex)
                {
                    Err = ex.Message;

                    Logger.ErrorWithException(ex);
                }
            }

            await ProgressStep(fileId: FolderDao.CanCalculateSubitems(fileId) ? default : fileId);
        }

        Result = sb.ToString();

        return needToMark;
    }

    private const int MoveBatchSize = 100;

    // Moves the plain files in chunks through FileDao.MoveFilesAsync and reports their
    // events. Returns the ids that must go through the regular per-file path instead:
    // name conflicts, forms, room/owner changes, permission errors.
    private async Task<List<T>> MoveFileBatchAsync<TTo>(
        AsyncServiceScope scope,
        List<T> fileIds,
        Folder<TTo> toFolder,
        List<Folder<TTo>> toParentFolders,
        List<FileEntry<TTo>> needToMark,
        StringBuilder sb,
        Func<T, Task<Folder<T>>> getParentFolder,
        bool checkPermissions)
    {
        var scopeClass = scope.ServiceProvider.GetService<FileMoveCopyOperationScope>();
        var (filesMessageService, fileMarker, _, _, _, _) = scopeClass;
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var webhookManager = scope.ServiceProvider.GetService<WebhookManager>();
        var webhookPublisher = scope.ServiceProvider.GetService<IWebhookPublisher>();
        var fileDao = scope.ServiceProvider.GetService<IFileDao<TTo>>();
        var folderDao = scope.ServiceProvider.GetService<IFolderDao<TTo>>();
        var permissionsManager = scope.ServiceProvider.GetService<PermissionCheckStarter<T, TTo>>();

        if (toFolder.RootFolderType == FolderType.VirtualRooms && toParentFolders.Exists(folder => folder.FolderType == FolderType.FillingFormsRoom))
        {
            // only forms are allowed there — the per-file path reports that properly
            return fileIds;
        }

        var toRoomId = (int)(object)(await folderDao.GetParentRoomInfoFromFileEntryAsync(toFolder)).RoomId;

        if (toRoomId != -1)
        {
            var toRoom = toFolder.IsRoom ? toFolder : await folderDao.GetFolderAsync((TTo)(object)toRoomId);
            if (toRoom == null || toRoom.SettingsIndexing)
            {
                // custom order in indexed rooms is maintained per file
                return fileIds;
            }
        }

        var fileTracker = scope.ServiceProvider.GetService<FileTrackerHelper>();
        var lockerManager = scope.ServiceProvider.GetService<LockerManager>();

        var fallback = new List<T>();
        var candidates = new List<(File<T> File, Folder<T> Parent)>();
        var parentRoomCache = new Dictionary<T, int>();

        var filesByIds = new Dictionary<T, File<T>>();

        await foreach (var file in FileDao.GetFilesAsync(fileIds))
        {
            filesByIds[file.Id] = file;
        }

        foreach (var fileId in fileIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            if (!filesByIds.TryGetValue(fileId, out var file) ||
                file.IsForm ||
                file.Encrypted ||
                file.ProviderEntry ||
                file.RootFolderType is FolderType.Privacy or FolderType.TRASH ||
                Equals(file.ParentId, toFolder.Id))
            {
                fallback.Add(fileId);
                continue;
            }

            var parent = await getParentFolder(file.ParentId);

            if (parent == null ||
                parent.RootFolderType is FolderType.TRASH ||
                parent.FolderType is FolderType.Knowledge ||
                parent.RootCreateBy != toFolder.RootCreateBy)
            {
                fallback.Add(fileId);
                continue;
            }

            if (!parentRoomCache.TryGetValue(parent.Id, out var fromRoomId))
            {
                fromRoomId = (int)(object)(await FolderDao.GetParentRoomInfoFromFileEntryAsync(parent)).RoomId;
                parentRoomCache.Add(parent.Id, fromRoomId);
            }

            if (fromRoomId != toRoomId)
            {
                fallback.Add(fileId);
                continue;
            }

            // CanMove is already part of the destination check, so only the lock remains here;
            // the editing state is re-checked right before each chunk is moved
            var err = await permissionsManager.CheckFilesPermissionsAsync(file, toFolder, toParentFolders, _copy, _resolveType);

            if (err == null && checkPermissions && await lockerManager.FileLockedForMeAsync(file.Id))
            {
                err = FilesCommonResource.ErrorMessage_LockedFile;
            }

            if (err != null)
            {
                // the regular path re-checks and reports the error
                fallback.Add(fileId);
                continue;
            }

            candidates.Add((file, parent));
        }

        if (candidates.Count == 0)
        {
            return fallback;
        }

        var toMove = candidates;

        // Duplicate mode moves files regardless of name collisions (same as the per-file
        // path, which skips conflict detection for it); otherwise — one conflict lookup
        // per destination instead of one per file, matching the case-insensitive db comparison.
        // Same-titled files within the selection itself would collide right after the first
        // of them is moved, so only one of them may go through the batch
        if (_resolveType != FileConflictResolveType.Duplicate)
        {
            var existingTitles = new HashSet<string>(
                await fileDao.GetExistingTitlesAsync(toFolder.Id, candidates.Select(c => c.File.Title)),
                StringComparer.OrdinalIgnoreCase);

            var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            toMove = [];

            foreach (var candidate in candidates)
            {
                if (existingTitles.Contains(candidate.File.Title) || !seenTitles.Add(candidate.File.Title))
                {
                    fallback.Add(candidate.File.Id);
                }
                else
                {
                    toMove.Add(candidate);
                }
            }
        }

        var anyWebhooks = (await webhookPublisher.GetWebhookConfigsAsync<File<TTo>>(WebhookTrigger.FileMoved, null, null)).Any();
        var isToFolder = Equals(toFolder.Id, _daoFolderId);

        foreach (var candidateChunk in toMove.Chunk(MoveBatchSize))
        {
            CancellationToken.ThrowIfCancellationRequested();

            // the editing state may have changed while previous chunks were being processed
            var chunk = new List<(File<T> File, Folder<T> Parent)>(candidateChunk.Length);

            foreach (var candidate in candidateChunk)
            {
                if (await fileTracker.IsEditingAsync(candidate.File.Id, false))
                {
                    fallback.Add(candidate.File.Id);
                }
                else
                {
                    chunk.Add(candidate);
                }
            }

            if (chunk.Count == 0)
            {
                continue;
            }

            var socketRecipients = new Dictionary<T, (IEnumerable<Guid> Users, IEnumerable<Guid> SharedUsers)>();

            foreach (var (file, _) in chunk)
            {
                socketRecipients[file.Id] = await socketManager.GetDeleteRecipientsAsync(file);
            }

            List<T> movedIds;

            try
            {
                movedIds = (await FileDao.MoveFilesAsync(
                    chunk.Select(c => c.File.Id),
                    (T)(object)toFolder.Id,
                    chunk.Select(c => c.Parent.Id).Distinct())).ToList();
            }
            catch (Exception ex)
            {
                // the batch transaction has rolled back: these files go the regular way
                Logger.ErrorWithException(ex);
                fallback.AddRange(chunk.Select(c => c.File.Id));
                continue;
            }

            // files whose parent changed concurrently were not touched by the batch
            var skipped = chunk.Where(c => !movedIds.Contains(c.File.Id)).ToList();
            fallback.AddRange(skipped.Select(c => c.File.Id));

            var moved = chunk.Where(c => movedIds.Contains(c.File.Id)).ToList();

            // unread markers are removed only for the files that were actually moved
            foreach (var (file, _) in moved)
            {
                await fileMarker.RemoveMarkAsNewForAllAsync(file);
            }

            var newFiles = new Dictionary<TTo, File<TTo>>();

            await foreach (var newFile in fileDao.GetFilesAsync(moved.Select(c => (TTo)(object)c.File.Id)))
            {
                newFiles[newFile.Id] = newFile;
            }

            foreach (var (file, parent) in moved)
            {
                if (!newFiles.TryGetValue((TTo)(object)file.Id, out var newFile))
                {
                    // moved by the batch but gone already (deleted concurrently) — the move
                    // itself happened, so the progress must still account for the file
                    Logger.InformationUnableFileMoveCopyOperation(file.Id.ToString(), "the file is missing after the batched move");
                    await ProgressStep(fileId: FolderDao.CanCalculateSubitems(file.Id) ? default : file.Id);
                    continue;
                }

                var (users, sharedUsers) = socketRecipients[file.Id];
                await socketManager.DeleteFileAsync(file, users: users, sharedUsers: sharedUsers);

                await filesMessageService.SendMoveMessageAsync(newFile, parent, toFolder, toParentFolders, false, _headers, [file.Title, parent.Title, toFolder.Title, toFolder.Id.ToString()]);

                if (anyWebhooks)
                {
                    await webhookManager.PublishAsync(WebhookTrigger.FileMoved, newFile);
                }

                if (isToFolder)
                {
                    // the gate guarantees the source and destination rooms are the same,
                    // so entries staying within one room are not marked as new
                    var withinRoom = file.RootFolderType == FolderType.VirtualRooms && toFolder.RootFolderType == FolderType.VirtualRooms;
                    if (!withinRoom)
                    {
                        needToMark.Add(newFile);
                    }
                }

                await socketManager.CreateFileAsync(newFile);

                if (ProcessedFile(file.Id))
                {
                    sb.Append($"file_{newFile.Id}{SplitChar}");
                }

                await ProgressStep(fileId: FolderDao.CanCalculateSubitems(file.Id) ? default : file.Id);
            }
        }

        return fallback;
    }
}

[Scope]
public record FileMoveCopyOperationScope(
    FilesMessageService FilesMessageService,
    FileMarker FileMarker,
    FileUtility FileUtility,
    Global Global,
    LockerManager LockerManager,
    ThumbnailSettings ThumbnailSettings);
