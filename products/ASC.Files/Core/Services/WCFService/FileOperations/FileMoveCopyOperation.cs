// (c) Copyright Ascensio System SIA 2009-2024
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
        JsonElement DestFolderId,
        bool Copy,
        FileConflictResolveType ResolveType,
        bool HoldResult = true,
        IDictionary<string, string> Headers = null,
        ExternalSessionSnapshot SessionSnapshot = null) : base(Folders, Files, TenantId, Headers, SessionSnapshot, HoldResult)
    {
        this.DestFolderId = DestFolderId.ToString();
        this.Copy = Copy;
        this.ResolveType = ResolveType;
    }

    [ProtoMember(7)]
    public string DestFolderId { get; init; }
    
    [ProtoMember(8)]
    public bool Copy { get; init; }
    
    [ProtoMember(9)]
    public FileConflictResolveType ResolveType { get; init; }
}

[Transient]
public class FileMoveCopyOperation(IServiceProvider serviceProvider) : ComposeFileOperation<FileMoveCopyOperationData<string>, FileMoveCopyOperationData<int>>(serviceProvider)
{
    protected override FileOperationType FileOperationType => FileOperationType.Copy;

    public void Init(bool holdResult, bool copy)
    {
        base.Init(holdResult);
        
        if (!copy)
        {
            this[OpType] = (int)FileOperationType.Move;
        }
    }

    public override void Init(FileMoveCopyOperationData<int> data, FileMoveCopyOperationData<string> thirdPartyData, string taskId)
    {
        base.Init(data, thirdPartyData, taskId);
        var copy = data?.Copy ?? thirdPartyData?.Copy ?? false;
        
        if (!copy)
        {
            this[OpType] = (int)FileOperationType.Move;
        }
    }

    public override Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        DaoOperation = new FileMoveCopyOperation<int>(_serviceProvider, Data);
        ThirdPartyOperation = new FileMoveCopyOperation<string>(_serviceProvider, ThirdPartyData);

        return base.RunJob(distributedTask, cancellationToken);

    }
}

class FileMoveCopyOperation<T> : FileOperation<FileMoveCopyOperationData<T>, T>
{
    private readonly int _daoFolderId;
    private readonly string _thirdPartyFolderId;
    private readonly bool _copy;
    private readonly FileConflictResolveType _resolveType;
    private readonly IDictionary<string, StringValues> _headers;
    private readonly Dictionary<T, Folder<T>> _parentRooms = new();

    public FileMoveCopyOperation(IServiceProvider serviceProvider, FileMoveCopyOperationData<T> data)
        : base(serviceProvider, data)
    {
        var toFolderId = data.DestFolderId;

        if (!int.TryParse(data.DestFolderId, out var i))
        {
            _thirdPartyFolderId = toFolderId;
        }
        else
        {
            _daoFolderId = i;
        }
    
        _copy = data.Copy;
        _resolveType = data.ResolveType;

        _headers = data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value));
        this[OpType] = (int)(_copy ? FileOperationType.Copy : FileOperationType.Move);
    }

    protected override async Task DoJob(IServiceScope serviceScope)
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

    private async Task DoAsync<TTo>(IServiceScope scope, TTo tto)
    {
        var fileMarker = scope.ServiceProvider.GetService<FileMarker>();
        var folderDao = scope.ServiceProvider.GetService<IFolderDao<TTo>>();
        var fileSecurity = scope.ServiceProvider.GetService<FileSecurity>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();

        //TODO: check on each iteration?
        var toFolder = await folderDao.GetFolderAsync(tto);
        if (toFolder == null)
        {
            return;
        }
        
        if (toFolder.FolderType != FolderType.VirtualRooms && toFolder.FolderType != FolderType.Archive && !await FilesSecurity.CanCreateAsync(toFolder))
        {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var parentFolders = await folderDao.GetParentFoldersAsync(toFolder.Id).ToListAsync();
        if (parentFolders.Exists(parent => Folders.Exists(r => r.ToString() == parent.Id.ToString())))
        {
            this[Err] = FilesCommonResource.ErrorMessage_FolderCopyError;

            return;
        }

        if (!_copy && (toFolder.FolderType == FolderType.FillingFormsRoom || parentFolders.Exists(parent => parent.FolderType == FolderType.FillingFormsRoom)))
        {
            if (Folders.Count > 0)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FolderMoveFormFillingError;

                return;
            }
            if (Files.Count > 1)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FilesMoveFormFillingError;

                return;
            }
        }

        var isRoom = false;

        if (0 < Folders.Count)
        {
            var firstFolder = await FolderDao.GetFolderAsync(Folders[0]);
            isRoom = DocSpaceHelper.IsRoom(firstFolder.FolderType);

            if (_copy && !await FilesSecurity.CanCopyAsync(firstFolder))
            {
               this[Err] = FilesCommonResource.ErrorMessage_SecurityException_CopyFolder;

               return;
            }
            
            if (!_copy && !await FilesSecurity.CanMoveAsync(firstFolder))
            {
                if (isRoom)
                {
                    this[Err] = toFolder.FolderType == FolderType.Archive
                        ? FilesCommonResource.ErrorMessage_SecurityException_ArchiveRoom
                        : FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom;

                }
                else
                {
                    this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                }

                return;
            }
        }

        if (0 < Files.Count)
        {
            var firstFile = await FileDao.GetFileAsync(Files[0]);

            if (_copy && !await FilesSecurity.CanCopyAsync(firstFile))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_CopyFile;

                return;
            }
            if (!_copy && !await FilesSecurity.CanMoveAsync(firstFile))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;

                return;
            }
        }

        if (_copy && !(isRoom && toFolder.FolderType == FolderType.VirtualRooms) && !await fileSecurity.CanCopyToAsync(toFolder))
        {
            this[Err] = FilesCommonResource.ErrorMessage_SecurityException_CopyToFolder;

            return;
        }
        
        if (!_copy && !await fileSecurity.CanMoveToAsync(toFolder))
        {
            this[Err] = toFolder.FolderType switch
            {
                FolderType.VirtualRooms => FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom,
                FolderType.Archive => FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom,
                _ => FilesCommonResource.ErrorMessage_SecurityException_MoveToFolder
            };

            return;
        }

        this[Res] += $"folder_{tto}{SplitChar}";
        
        var needToMark = new List<FileEntry>();

        var moveOrCopyFoldersTask = await MoveOrCopyFoldersAsync(scope, Folders, toFolder, _copy, parentFolders);
        var moveOrCopyFilesTask = await MoveOrCopyFilesAsync(scope, Files, toFolder, _copy, parentFolders);

        needToMark.AddRange(moveOrCopyFilesTask);

        foreach (var folder in moveOrCopyFoldersTask)
        {
            if (toFolder.FolderType != FolderType.Archive && !DocSpaceHelper.IsRoom(folder.FolderType))
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

    private async Task<List<File<TTo>>> GetFilesAsync<TTo>(IServiceScope scope, Folder<TTo> folder)
    {
        var fileDao = scope.ServiceProvider.GetService<IFileDao<TTo>>();

        var files = await fileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true).ToListAsync();

        return files;
    }

    private async Task<List<Folder<TTo>>> MoveOrCopyFoldersAsync<TTo>(IServiceScope scope, List<T> folderIds, Folder<TTo> toFolder, bool copy, IEnumerable<Folder<TTo>> toFolderParents, bool checkPermissions = true)
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
        var userManager = scope.ServiceProvider.GetService<UserManager>();
        var tenantQuotaFeatureStatHelper = scope.ServiceProvider.GetService<TenantQuotaFeatureStatHelper>();
        var quotaSocketManager = scope.ServiceProvider.GetService<QuotaSocketManager>();
        var settingsManager = scope.ServiceProvider.GetService<SettingsManager>();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var quotaService = scope.ServiceProvider.GetService<IQuotaService>();
        var cache = scope.ServiceProvider.GetService<ICache>();
        var distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var roomLogoManager = scope.ServiceProvider.GetRequiredService<RoomLogoManager>();
        var global = scope.ServiceProvider.GetRequiredService<Global>();
        var fileSecurity = scope.ServiceProvider.GetRequiredService<FileSecurity>();

        var toFolderId = toFolder.Id;
        var isToFolder = Equals(toFolderId, _daoFolderId);

        var sb = new StringBuilder();
        sb.Append(this[Res]);
        foreach (var folderId in folderIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var folder = await FolderDao.GetFolderAsync(folderId);
            var cacheKey = "parentRoomInfo" + folder.ParentId;

            var parentRoomId = cache.Get<string>(cacheKey);

            if (parentRoomId == null)
            {
                var (rId, _) = await FolderDao.GetParentRoomInfoFromFileEntryAsync(folder);
                cache.Insert(cacheKey, rId.ToString(), TimeSpan.FromMinutes(5));
                parentRoomId = rId.ToString();
            }

            var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);
            var isThirdPartyRoom = isRoom && folder.ProviderEntry;

            var canMoveOrCopy = (copy && await FilesSecurity.CanCopyAsync(folder)) || (!copy && await FilesSecurity.CanMoveAsync(folder));
            checkPermissions = isRoom ? !canMoveOrCopy : checkPermissions;

            var canUseRoomQuota = true;
            var canUseUserQuota = true;
            long roomQuotaLimit = 0;
            long userQuotaLimit = 0;

            var toFolderRoom = toFolderParents.FirstOrDefault(f => DocSpaceHelper.IsRoom(f.FolderType));

            if (!isRoom &&
                toFolderRoom != null &&
                !string.Equals(parentRoomId, toFolderRoom.Id.ToString()))
            {
                var quotaRoomSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
                if (quotaRoomSettings.EnableQuota)
                {
                    roomQuotaLimit = toFolderRoom.SettingsQuota == TenantEntityQuotaSettings.DefaultQuotaValue ? quotaRoomSettings.DefaultQuota : toFolderRoom.SettingsQuota;
                    if (roomQuotaLimit != TenantEntityQuotaSettings.NoQuota)
                    {
                        if (roomQuotaLimit - toFolderRoom.Counter < folder.Counter)
                        {
                            canUseRoomQuota = false;
                        }
                    }
                }
            }
            if (!isRoom &&
                toFolderRoom == null &&
                (int.TryParse(parentRoomId, out var curRId) && curRId != -1) &&
                (toFolder.FolderType == FolderType.USER || toFolder.FolderType == FolderType.DEFAULT))
            {
                var tenantId = await tenantManager.GetCurrentTenantIdAsync();
                var quotaUserSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
                if (quotaUserSettings.EnableQuota)
                {
                    var user = await userManager.GetUsersAsync(toFolder.RootCreateBy);
                    var userQuotaData = await settingsManager.LoadAsync<UserQuotaSettings>(user);
                    userQuotaLimit = userQuotaData.UserQuota == userQuotaData.GetDefault().UserQuota ? quotaUserSettings.DefaultQuota : userQuotaData.UserQuota;
                    var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenantId, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));
                    if (userQuotaLimit != TenantEntityQuotaSettings.NoQuota)
                    {
                        if (userQuotaLimit - userUsedSpace < folder.Counter)
                        {
                            canUseUserQuota = false;
                        }
                    }
                }
            }

            if (folder == null)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FolderNotFound;
            }
            else if (copy && checkPermissions && !canMoveOrCopy)
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_CopyFolder;
            }
            else if (!copy && checkPermissions && !canMoveOrCopy)
            {
                if (isRoom)
                {
                    this[Err] = toFolder.FolderType == FolderType.Archive
                        ? FilesCommonResource.ErrorMessage_SecurityException_ArchiveRoom
                        : FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom;
                }
                else
                {
                    this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                }
            }
            else if (!isRoom && (toFolder.FolderType == FolderType.VirtualRooms || toFolder.RootFolderType == FolderType.Archive))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            }
            else if (isRoom && toFolder.FolderType != FolderType.VirtualRooms && toFolder.FolderType != FolderType.Archive)
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom;
            }
            else if (!isRoom && folder.SettingsPrivate && !await CompliesPrivateRoomRulesAsync(folder, toFolderParents))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            }
            else if (checkPermissions && folder.RootFolderType != FolderType.TRASH && !await FilesSecurity.CanDownloadAsync(folder))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException;
            }
            else if (folder.RootFolderType == FolderType.Privacy
                && (copy || toFolder.RootFolderType != FolderType.Privacy))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            }
            else if (!canUseRoomQuota)
            {
                this[Err] = FileSizeComment.GetRoomFreeSpaceException(roomQuotaLimit);
            }
            else if (!canUseUserQuota)
            {
                this[Err] = FileSizeComment.GetUserFreeSpaceException(userQuotaLimit);
            }
            else if (!Equals(folder.ParentId ?? default, toFolderId) || _resolveType == FileConflictResolveType.Duplicate)
            {
                var files = await FileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true).ToListAsync();
                var (isError, message) = await WithErrorAsync(scope, files, checkPermissions);

                try
                {
                    //if destination folder contains folder with same name then merge folders
                    var conflictFolder = (folder.RootFolderType == FolderType.Privacy || isRoom|| 
                                          (!Equals(folder.ParentId ?? default, toFolderId) && _resolveType == FileConflictResolveType.Duplicate))
                        ? null
                        : await folderDao.GetFolderAsync(folder.Title, toFolderId);
                    Folder<TTo> newFolder;

                    if (copy || conflictFolder != null)
                    {
                        if (conflictFolder != null)
                        {
                            if (!conflictFolder.ProviderEntry)
                            {
                                conflictFolder.Id = default;
                                conflictFolder.Title = await global.GetAvailableTitleAsync(conflictFolder.Title, conflictFolder.ParentId, folderDao.IsExistAsync);
                                conflictFolder.Id = await folderDao.SaveFolderAsync(conflictFolder);
                            }
                            
                            newFolder = conflictFolder;
                            
                            if (isToFolder)
                            {
                                needToMark.Add(conflictFolder);
                            }
                        }
                        else
                        {
                            var title = await global.GetAvailableTitleAsync(folder.Title, toFolderId, folderDao.IsExistAsync);
                            newFolder = await FolderDao.CopyFolderAsync(folder.Id, toFolderId, CancellationToken);
                            newFolder.Title = title;
                            newFolder.Id = await folderDao.SaveFolderAsync(newFolder);
                            
                            if (isRoom && Equals(folder.ParentId ?? default, toFolderId))
                            {
                                if (await roomLogoManager.CopyAsync(folder, newFolder))
                                {
                                    newFolder.SettingsHasLogo = true;
                                    await folderDao.SaveFolderAsync(newFolder);
                                }

                                var primaryExternalLink = (await FilesSecurity.GetSharesAsync(folder)).FirstOrDefault(r => r.SubjectType == SubjectType.PrimaryExternalLink);
                                if (primaryExternalLink != null)
                                {
                                    await fileSecurity.ShareAsync(newFolder.Id, newFolder.FileEntryType, Guid.NewGuid(), primaryExternalLink.Share, primaryExternalLink.SubjectType, primaryExternalLink.Options);
                                }
                            }
                            
                            await filesMessageService.SendAsync(MessageAction.FolderCopied, newFolder, toFolder, _headers, newFolder.Title, toFolder.Title, toFolder.Id.ToString());

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
                            await MoveOrCopyFilesAsync(scope, await FileDao.GetFilesAsync(folder.Id).ToListAsync(), newFolder, copy, toFolderParents, checkPermissions);
                            await MoveOrCopyFoldersAsync(scope, await FolderDao.GetFoldersAsync(folder.Id).Select(f => f.Id).ToListAsync(), newFolder, copy, toFolderParents, checkPermissions);

                            if (!copy)
                            {
                                if (checkPermissions && !await FilesSecurity.CanMoveAsync(folder))
                                {
                                    this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
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
                                TTo newFolderId;
                                if (copy)
                                {
                                    newFolder = await FolderDao.CopyFolderAsync(folder.Id, toFolderId, CancellationToken);
                                    newFolderId = newFolder.Id;
                                    await filesMessageService.SendAsync(MessageAction.FolderCopiedWithOverwriting, newFolder, toFolder, _headers, newFolder.Title, toFolder.Title, toFolder.Id.ToString());

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
                                    this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                                }
                                else if (isError)
                                {
                                    this[Err] = message;
                                }
                                else
                                {
                                    await fileMarker.RemoveMarkAsNewForAllAsync(folder);

                                    newFolderId = await FolderDao.MoveFolderAsync(folder.Id, toFolderId, CancellationToken);
                                    newFolder = await folderDao.GetFolderAsync(newFolderId);
                                    await filesMessageService.SendAsync(MessageAction.FolderMovedWithOverwriting, folder, toFolder, _headers, folder.Title, toFolder.Title, toFolder.Id.ToString());

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
                            this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                        }
                        else if (isError)
                        {
                            this[Err] = message;
                        }
                        else
                        {
                            await fileMarker.RemoveMarkAsNewForAllAsync(folder);
                            var parentFolder = await FolderDao.GetFolderAsync(folder.RootId);

                            TTo newFolderId = default;

                            if (isThirdPartyRoom)
                            {
                                await ProviderDao.UpdateRoomProviderInfoAsync(new ProviderData
                                {
                                    Id = folder.ProviderId,
                                    RootFolderType = toFolder.FolderType
                                });

                                await socketManager.DeleteFolder(folder);

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

                                            await socketManager.DeleteFolder(folder, action: async () =>
                                            {
                                                newFolderId = await FolderDao.MoveFolderAsync(folder.Id, toFolderId, CancellationToken);
                                            });
                                        
                                            var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                                            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                                        }
                                    }
                                    else
                                    {
                                        newFolderId = await FolderDao.MoveFolderAsync(folder.Id, toFolderId, CancellationToken);
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
                                    var pins = await TagDao.GetTagsAsync(Guid.Empty, TagType.Pin, new List<FileEntry<T>> { folder }).ToListAsync();
                                    if (pins.Count > 0)
                                    {
                                        await TagDao.RemoveTagsAsync(pins);
                                    }

                                    await filesMessageService.SendAsync(MessageAction.RoomArchived, folder, _headers, folder.Title);
                                }
                                else
                                {
                                    await filesMessageService.SendAsync(MessageAction.RoomUnarchived, folder, _headers, folder.Title);
                                }
                            }
                            else
                            {
                                await filesMessageService.SendAsync(MessageAction.FolderMoved, folder, toFolder, _headers, folder.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString());
                            }


                            if (isToFolder)
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
                    this[Res] = sb.ToString();
                }
                catch (Exception ex)
                {
                    this[Err] = ex.Message;

                    Logger.ErrorWithException(ex);
                }
            }

            await ProgressStep(FolderDao.CanCalculateSubitems(folderId) ? default : folderId);
        }

        return needToMark;
    }

    private async Task<List<FileEntry<TTo>>> MoveOrCopyFilesAsync<TTo>(IServiceScope scope, List<T> fileIds, Folder<TTo> toFolder, bool copy, IEnumerable<Folder<TTo>> toParentFolders, bool checkPermissions = true)
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
        var globalStorage = scope.ServiceProvider.GetService<GlobalStore>();
        var fileStorageService = scope.ServiceProvider.GetService<FileStorageService>();
        var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

        var toFolderId = toFolder.Id;
        var sb = new StringBuilder();
        var isPdfForm = false;
        var numberRoomMembers = 0;
        foreach (var fileId in fileIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var file = await FileDao.GetFileAsync(fileId);
            var (isError, message) = await WithErrorAsync(scope, new[] { file }, checkPermissions);

            if (file == null)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FileNotFound;
            }
            else if (toFolder.FolderType == FolderType.VirtualRooms || toFolder.RootFolderType == FolderType.Archive)
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            }
            else if (copy && !await FilesSecurity.CanCopyAsync(file))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_CopyFile;
            }
            else if (!copy && checkPermissions && !await FilesSecurity.CanMoveAsync(file))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            }
            else if (checkPermissions && file.RootFolderType != FolderType.TRASH && !await FilesSecurity.CanDownloadAsync(file))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException;
            }
            else if (!await CompliesPrivateRoomRulesAsync(file, toParentFolders))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            }
            else if (file.RootFolderType == FolderType.Privacy
                && (copy || toFolder.RootFolderType != FolderType.Privacy))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            }
            else if (global.EnableUploadFilter
                     && !fileUtility.ExtsUploadable.Contains(FileUtility.GetFileExtension(file.Title)))
            {
                this[Err] = FilesCommonResource.ErrorMessage_NotSupportedFormat;
            }
            else
            {
                if (toFolder.RootFolderType == FolderType.VirtualRooms) {
                    var folderDao = scope.ServiceProvider.GetService<IFolderDao<TTo>>();
                    var (rId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(toFolder);
                    if (int.TryParse(rId.ToString(), out var roomId) && roomId != -1)
                    {
                        var room = await folderDao.GetFolderAsync((TTo)Convert.ChangeType(roomId, typeof(TTo)));
                        if (room.FolderType == FolderType.FillingFormsRoom)
                        {
                            var extension = FileUtility.GetFileExtension(file.Title);
                            var fileType = FileUtility.GetFileTypeByExtention(extension);
                            if (fileType != FileType.Pdf || (fileType == FileType.Pdf && !await fileStorageService.CheckExtendedPDF(file)))
                            {
                                this[Err] = _copy ? FilesCommonResource.ErrorMessage_UploadToFormRoom : FilesCommonResource.ErrorMessage_MoveToFormRoom;
                                continue;
                            }
                            else if (fileType == FileType.Pdf)
                            {
                                isPdfForm = true;
                                numberRoomMembers = await fileStorageService.GetPureSharesCountAsync(toFolder.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, "");
                            }
                        }
                    }
                }
                var deleteLinks = file.RootFolderType == FolderType.USER &&
                                toFolder.RootFolderType is FolderType.VirtualRooms or FolderType.Archive or FolderType.TRASH;

                var parentFolder = await FolderDao.GetFolderAsync(file.ParentId);
                try
                {
                    var conflict = _resolveType == FileConflictResolveType.Duplicate
                        || file.RootFolderType == FolderType.Privacy || file.Encrypted
                                       ? null
                                       : await fileDao.GetFileAsync(toFolderId, file.Title);
                    var fileType = FileUtility.GetFileTypeByFileName(file.Title);

                    if (conflict == null)
                    {
                        File<TTo> newFile = null;
                        if (copy)
                        {
                            try
                            {
                                var title = await global.GetAvailableTitleAsync(file.Title, toFolderId, fileDao.IsExistAsync);
                                newFile = await FileDao.CopyFileAsync(file.Id, toFolderId); //Stream copy will occur inside dao
                                newFile.Title = title;
                                await fileDao.SaveFileAsync(newFile, null);
                                await filesMessageService.SendAsync(MessageAction.FileCopied, newFile, toFolder, _headers, newFile.Title, parentFolder.Title, toFolder.Title, toFolder.ToString());

                                needToMark.Add(newFile);

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
                            if (isError)
                            {
                                this[Err] = message;
                            }
                            else
                            {
                                await fileMarker.RemoveMarkAsNewForAllAsync(file);

                                TTo newFileId = default;
                                await socketManager.DeleteFileAsync(file, action: async () => newFileId = await FileDao.MoveFileAsync(file.Id, toFolderId, deleteLinks));
                                newFile = await fileDao.GetFileAsync(newFileId);

                                await filesMessageService.SendAsync(MessageAction.FileMoved, file, toFolder, _headers, file.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString());

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
                                    needToMark.Add(newFile);
                                }

                                if (fileType == FileType.Pdf)
                                {
                                    await LinkDao.DeleteAllLinkAsync(file.Id);
                                    await FileDao.SaveProperties(file.Id, null);
                                }

                                await socketManager.CreateFileAsync(newFile);
                                if (isPdfForm)
                                {
                                    var properties = await fileDao.GetProperties(newFile.Id) ?? new EntryProperties() { FormFilling = new FormFillingProperties()};
                                    properties.FormFilling.StartFilling = true;
                                    properties.FormFilling.CollectFillForm = true;
                                    await fileDao.SaveProperties(newFile.Id, properties);
                                    await socketManager.CreateFormAsync(newFile, securityContext.CurrentAccount.ID, numberRoomMembers <= 1);
                                };

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
                                this[Err] = FilesCommonResource.ErrorMessage_SecurityException;
                            }
                            else if (await lockerManager.FileLockedForMeAsync(conflict.Id))
                            {
                                this[Err] = FilesCommonResource.ErrorMessage_LockedFile;
                            }
                            else if (await fileTracker.IsEditingAsync(conflict.Id))
                            {
                                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_UpdateEditingFile;
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
                                    foreach (var size in thumbnailSettings.Sizes)
                                    {
                                        await (await globalStorage.GetStoreAsync()).CopyAsync(String.Empty,
                                                                                FileDao.GetUniqThumbnailPath(file, size.Width, size.Height),
                                                                                String.Empty,
                                                                                fileDao.GetUniqThumbnailPath(newFile, size.Width, size.Height));
                                    }

                                    await fileDao.SetThumbnailStatusAsync(newFile, Thumbnail.Created);

                                    newFile.ThumbnailStatus = Thumbnail.Created;
                                }

                                await linkDao.DeleteAllLinkAsync(newFile.Id);

                                needToMark.Add(newFile);

                                await socketManager.CreateFileAsync(newFile);
                                if (isPdfForm) await socketManager.CreateFormAsync(newFile, securityContext.CurrentAccount.ID, numberRoomMembers <= 1);

                                if (copy)
                                {
                                    await filesMessageService.SendAsync(MessageAction.FileCopiedWithOverwriting, newFile, toFolder, _headers, newFile.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString());
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
                                        if (isError)
                                        {
                                            this[Err] = message;
                                        }
                                        else
                                        {
                                            await socketManager.DeleteFileAsync(file, action: async () =>
                                            {
                                                await FileDao.DeleteFileAsync(file.Id);

                                                await LinkDao.DeleteAllLinkAsync(file.Id);
                                            });

                                            await filesMessageService.SendAsync(MessageAction.FileMovedWithOverwriting, file, toFolder, _headers, file.Title, parentFolder.Title, toFolder.Title, toFolder.Id.ToString());

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
                catch (Exception ex)
                {
                    this[Err] = ex.Message;

                    Logger.ErrorWithException(ex);
                }
            }

            await ProgressStep(fileId: FolderDao.CanCalculateSubitems(fileId) ? default : fileId);
        }

        this[Res] = sb.ToString();

        return needToMark;
    }

    private async Task<(bool isError, string message)> WithErrorAsync(IServiceScope scope, IEnumerable<File<T>> files, bool checkPermissions = true)
    {
        var lockerManager = scope.ServiceProvider.GetService<LockerManager>();
        var fileTracker = scope.ServiceProvider.GetService<FileTrackerHelper>();
        string error = null;
        foreach (var file in files)
        {
            if (checkPermissions && !await FilesSecurity.CanMoveAsync(file))
            {
                error = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;

                return (true, error);
            }
            if (checkPermissions && await lockerManager.FileLockedForMeAsync(file.Id))
            {
                error = FilesCommonResource.ErrorMessage_LockedFile;

                return (true, error);
            }
            if (await fileTracker.IsEditingAsync(file.Id))
            {
                error = FilesCommonResource.ErrorMessage_SecurityException_UpdateEditingFile;

                return (true, error);
            }
        }
        return (false, error);
    }

    private async Task<bool> CompliesPrivateRoomRulesAsync<TTo>(FileEntry<T> entry, IEnumerable<Folder<TTo>> toFolderParents)
    {
        Folder<T> entryParentRoom;

        if (_parentRooms.ContainsKey(entry.ParentId))
        {
            entryParentRoom = _parentRooms.Get(entry.ParentId);
        }
        else
        {
            entryParentRoom = await FolderDao.GetParentFoldersAsync(entry.ParentId).FirstOrDefaultAsync(f => f.SettingsPrivate && DocSpaceHelper.IsRoom(f.FolderType));
            _parentRooms.Add(entry.ParentId, entryParentRoom);
        }

        var toFolderParentRoom = toFolderParents.FirstOrDefault(f => f.SettingsPrivate && DocSpaceHelper.IsRoom(f.FolderType));


        if (entryParentRoom == null)
        {
            if(toFolderParentRoom == null)
            {
                return true;
            }

            return false;
        }

        if(toFolderParentRoom == null)
        {
            return false;
        }


        return entryParentRoom.Id.Equals(toFolderParentRoom.Id) && !_copy;
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
