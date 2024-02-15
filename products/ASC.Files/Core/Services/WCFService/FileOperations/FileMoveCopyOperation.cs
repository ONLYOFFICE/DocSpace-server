// (c) Copyright Ascensio System SIA 2010-2023
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

record FileMoveCopyOperationData<T>(
    IEnumerable<T> Folders,
    IEnumerable<T> Files,
    int TenantId,
    JsonElement DestFolderId,
    bool Copy,
    FileConflictResolveType ResolveType,
    bool HoldResult = true,
    IDictionary<string, string> Headers = null)
    : FileOperationData<T>(Folders, Files, TenantId, Headers, HoldResult);

[Transient]
class FileMoveCopyOperation(IServiceProvider serviceProvider) : ComposeFileOperation<FileMoveCopyOperationData<string>, FileMoveCopyOperationData<int>>(serviceProvider)
{
    protected override FileOperationType FileOperationType => FileOperationType.Copy;

    public override void Init<T>(T data)
    {
        base.Init(data);
        
        if (data is FileMoveCopyOperationData<JsonElement> { Copy: false })
        {
            this[OpType] = FileOperationType.Move;
        }
    }

    public override T Init<T>(string jsonData, string taskId)
    {
        var data  = base.Init<T>(jsonData, taskId);
        
        if (data is FileMoveCopyOperationData<JsonElement> { Copy: false })
        {
            this[OpType] = FileOperationType.Move;
        }

        return data;
    }

    public override Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Deserialize<FileMoveCopyOperationData<JsonElement>>((string)this[Data]);
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(data.Folders);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(data.Files);
        
        DaoOperation =  new FileMoveCopyOperation<int>(_serviceProvider, new FileMoveCopyOperationData<int>(folderIntIds, fileIntIds, data.TenantId, data.DestFolderId, data.Copy, data.ResolveType, data.HoldResult, data.Headers));
        ThirdPartyOperation = new FileMoveCopyOperation<string>(_serviceProvider, new FileMoveCopyOperationData<string>(folderStringIds, fileStringIds, data.TenantId, data.DestFolderId, data.Copy, data.ResolveType, data.HoldResult, data.Headers));
        
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
        
        if (toFolderId.ValueKind == JsonValueKind.String)
        {
            if (!int.TryParse(toFolderId.GetString(), out var i))
            {
                _thirdPartyFolderId = toFolderId.GetString();
            }
            else
            {
                _daoFolderId = i;
            }
        }
        else if (toFolderId.ValueKind == JsonValueKind.Number)
        {
            _daoFolderId = toFolderId.GetInt32();
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

        if (0 < Folders.Count)
        {
            var firstFolder = await FolderDao.GetFolderAsync(Folders[0]);
            var isRoom = DocSpaceHelper.IsRoom(firstFolder.FolderType);

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

        if (_copy && !await fileSecurity.CanCopyToAsync(toFolder))
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
        var tenantQuotaFeatureStatHelper = scope.ServiceProvider.GetService<TenantQuotaFeatureStatHelper>();
        var quotaSocketManager = scope.ServiceProvider.GetService<QuotaSocketManager>();
        var distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();

        var toFolderId = toFolder.Id;
        var isToFolder = Equals(toFolderId, _daoFolderId);

        var sb = new StringBuilder();
        sb.Append(this[Res]);
        foreach (var folderId in folderIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var folder = await FolderDao.GetFolderAsync(folderId);

            var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);
            var isThirdPartyRoom = isRoom && folder.ProviderEntry;

            var canMoveOrCopy = (copy && await FilesSecurity.CanCopyAsync(folder)) || (!copy && await FilesSecurity.CanMoveAsync(folder));
            checkPermissions = isRoom ? !canMoveOrCopy : checkPermissions;

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
                            newFolder = conflictFolder;

                            if (isToFolder)
                            {
                                needToMark.Add(conflictFolder);
                            }
                        }
                        else
                        {
                            newFolder = await FolderDao.CopyFolderAsync(folder.Id, toFolderId, CancellationToken);
                            await filesMessageService.SendAsync(MessageAction.FolderCopied, newFolder, toFolder, _headers, newFolder.Title, toFolder.Title);

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
                                    await filesMessageService.SendAsync(MessageAction.FolderCopiedWithOverwriting, newFolder, toFolder, _headers, newFolder.Title, toFolder.Title);

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
                                    await filesMessageService.SendAsync(MessageAction.FolderMovedWithOverwriting, folder, toFolder, _headers, folder.Title, toFolder.Title);

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
                                await filesMessageService.SendAsync(MessageAction.FolderMoved, folder, toFolder, _headers, folder.Title, parentFolder.Title, toFolder.Title);
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

            ProgressStep(FolderDao.CanCalculateSubitems(folderId) ? default : folderId);
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
        var (filesMessageService, fileMarker, fileUtility, global, entryManager, _thumbnailSettings) = scopeClass;
        var fileDao = scope.ServiceProvider.GetService<IFileDao<TTo>>();
        var fileTracker = scope.ServiceProvider.GetService<FileTrackerHelper>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var globalStorage = scope.ServiceProvider.GetService<GlobalStore>();

        var toFolderId = toFolder.Id;
        var sb = new StringBuilder();
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
                                newFile = await FileDao.CopyFileAsync(file.Id, toFolderId); //Stream copy will occur inside dao
                                await filesMessageService.SendAsync(MessageAction.FileCopied, newFile, toFolder, _headers, newFile.Title, parentFolder.Title, toFolder.Title);

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

                                await filesMessageService.SendAsync(MessageAction.FileMoved, file, toFolder, _headers, file.Title, parentFolder.Title, toFolder.Title);

                                if (file.RootFolderType == FolderType.TRASH && newFile.ThumbnailStatus == Thumbnail.NotRequired)
                                {
                                    newFile.ThumbnailStatus = Thumbnail.Waiting;

                                    await fileDao.SetThumbnailStatusAsync(newFile, Thumbnail.Waiting);
                                }

                                if (newFile.ProviderEntry)
                                {
                                    await LinkDao.DeleteAllLinkAsync(file.Id.ToString());
                                }

                                if (Equals(toFolderId, _daoFolderId))
                                {
                                    needToMark.Add(newFile);
                                }

                                if (fileType == FileType.Pdf)
                                {
                                    await LinkDao.DeleteAllLinkAsync(file.Id.ToString());
                                    await FileDao.SaveProperties(file.Id, null);
                                }
                                
                                await socketManager.CreateFileAsync(newFile);

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
                            else if (await entryManager.FileLockedForMeAsync(conflict.Id))
                            {
                                this[Err] = FilesCommonResource.ErrorMessage_LockedFile;
                            }
                            else if (fileTracker.IsEditing(conflict.Id))
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
                                newFile.ThumbnailStatus = file.ThumbnailStatus == Thumbnail.Created ? Thumbnail.Creating : Thumbnail.Waiting;


                                await using (var stream = await FileDao.GetFileStreamAsync(file))
                                {
                                    newFile.ContentLength = stream.CanSeek ? stream.Length : file.ContentLength;

                                    newFile = await fileDao.SaveFileAsync(newFile, stream);
                                }

                                if (file.ThumbnailStatus == Thumbnail.Created)
                                {
                                    foreach (var size in _thumbnailSettings.Sizes)
                                    {
                                        await (await globalStorage.GetStoreAsync()).CopyAsync(String.Empty,
                                                                                FileDao.GetUniqThumbnailPath(file, size.Width, size.Height),
                                                                                String.Empty,
                                                                                fileDao.GetUniqThumbnailPath(newFile, size.Width, size.Height));
                                    }

                                    await fileDao.SetThumbnailStatusAsync(newFile, Thumbnail.Created);

                                    newFile.ThumbnailStatus = Thumbnail.Created;
                                }

                                await LinkDao.DeleteAllLinkAsync(newFile.Id.ToString());

                                needToMark.Add(newFile);

                                await socketManager.CreateFileAsync(newFile);

                                if (copy)
                                {
                                    await filesMessageService.SendAsync(MessageAction.FileCopiedWithOverwriting, newFile, toFolder, _headers, newFile.Title, parentFolder.Title, toFolder.Title);
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

                                            await LinkDao.DeleteAllLinkAsync(file.Id.ToString());
                                            });

                                            await filesMessageService.SendAsync(MessageAction.FileMovedWithOverwriting, file, toFolder, _headers, file.Title, parentFolder.Title, toFolder.Title);

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

            ProgressStep(fileId: FolderDao.CanCalculateSubitems(fileId) ? default : fileId);
        }

        this[Res] = sb.ToString();

        return needToMark;
    }

    private async Task<(bool isError, string message)> WithErrorAsync(IServiceScope scope, IEnumerable<File<T>> files, bool checkPermissions = true)
    {
        var entryManager = scope.ServiceProvider.GetService<EntryManager>();
        var fileTracker = scope.ServiceProvider.GetService<FileTrackerHelper>();
        string error = null;
        foreach (var file in files)
        {
            if (checkPermissions && !await FilesSecurity.CanMoveAsync(file))
            {
                error = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;

                return (true, error);
            }
            if (checkPermissions && await entryManager.FileLockedForMeAsync(file.Id))
            {
                error = FilesCommonResource.ErrorMessage_LockedFile;

                return (true, error);
            }
            if (fileTracker.IsEditing(file.Id))
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
    EntryManager EntryManager,
    ThumbnailSettings ThumbnailSettings);
