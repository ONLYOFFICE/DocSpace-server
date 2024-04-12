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
public record FileDeleteOperationData<T> : FileOperationData<T>
{
    [ProtoMember(7)]
    public bool IgnoreException { get; set; }
    
    [ProtoMember(8)]
    public bool Immediately { get; set; }
    
    [ProtoMember(9)]
    public bool IsEmptyTrash { get; set; }

    public FileDeleteOperationData()
    {
        
    }

    public FileDeleteOperationData(IEnumerable<T> folders,
        IEnumerable<T> files,
        int tenantId,
        IDictionary<string, string> headers,
        ExternalSessionSnapshot sessionSnapshot,
        bool holdResult = true,
        bool ignoreException = false,
        bool immediately = false,
        bool isEmptyTrash = false) : base(folders, files, tenantId, headers, sessionSnapshot, holdResult)
    {
        IgnoreException = ignoreException;
        Immediately = immediately;
        IsEmptyTrash = isEmptyTrash;
    }
}

[Transient]
public class FileDeleteOperation(IServiceProvider serviceProvider) : ComposeFileOperation<FileDeleteOperationData<string>, FileDeleteOperationData<int>>(serviceProvider)
{
    protected override FileOperationType FileOperationType { get => FileOperationType.Delete; }

    public override Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        DaoOperation = new FileDeleteOperation<int>(_serviceProvider, Data);
        ThirdPartyOperation = new FileDeleteOperation<string>(_serviceProvider, ThirdPartyData);

        return base.RunJob(distributedTask, cancellationToken);
    }
}

class FileDeleteOperation<T> : FileOperation<FileDeleteOperationData<T>, T>
{
    private int _trashId;
    private readonly bool _ignoreException;
    private readonly bool _immediately;
    private readonly bool _isEmptyTrash;
    private readonly IDictionary<string, StringValues> _headers;

    public FileDeleteOperation(IServiceProvider serviceProvider, FileDeleteOperationData<T> fileOperationData)
    : base(serviceProvider, fileOperationData)
    {
        _ignoreException = fileOperationData.IgnoreException;
        _immediately = fileOperationData.Immediately;
        _headers = fileOperationData.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value));
        _isEmptyTrash = fileOperationData.IsEmptyTrash;
        this[OpType] = (int)FileOperationType.Delete;
    }

    protected override async Task DoJob(IServiceScope serviceScope)
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
            this[Res] += string.Format("folder_{0}{1}", root.Id, SplitChar);
        }
        if (_isEmptyTrash)
        {
            await DeleteFilesAsync(Files, serviceScope);
            await DeleteFoldersAsync(Folders, serviceScope);

            var trash = await folderDao.GetFolderAsync(_trashId);
            await filesMessageService.SendAsync(MessageAction.TrashEmptied, trash, _headers);
        }
        else
        {
            await DeleteFilesAsync(Files, serviceScope, true);
            await DeleteFoldersAsync(Folders, serviceScope, true);
        }
    }

    private async Task DeleteFoldersAsync(IEnumerable<T> folderIds, IServiceScope scope, bool isNeedSendActions = false, bool checkPermissions = true)
    {
        var scopeClass = scope.ServiceProvider.GetService<FileDeleteOperationScope>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var fileSharing = scope.ServiceProvider.GetService<FileSharing>();
        var authContext = scope.ServiceProvider.GetService<AuthContext>();
        var notifyClient = scope.ServiceProvider.GetService<NotifyClient>();

        var (fileMarker, filesMessageService, roomLogoManager) = scopeClass;
        roomLogoManager.EnableAudit = false;
        
        foreach (var folderId in folderIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var folder = await FolderDao.GetFolderAsync(folderId);
            var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);

            var canDelete = await FilesSecurity.CanDeleteAsync(folder);
            checkPermissions = isRoom ? !canDelete : checkPermissions;

            T canCalculate = default;
            if (folder == null)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FolderNotFound;
            }
            else if (folder.FolderType != FolderType.DEFAULT && folder.FolderType != FolderType.BUNCH
                && !DocSpaceHelper.IsRoom(folder.FolderType))
            {
                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_DeleteFolder;
            }
            else if (!_ignoreException && checkPermissions && !canDelete)
            {
                canCalculate = FolderDao.CanCalculateSubitems(folderId) ? default : folderId;

                this[Err] = FilesCommonResource.ErrorMessage_SecurityException_DeleteFolder;
            }
            else
            {
                canCalculate = FolderDao.CanCalculateSubitems(folderId) ? default : folderId;

                await fileMarker.RemoveMarkAsNewForAllAsync(folder);
                if (folder.ProviderEntry && ((folder.Id.Equals(folder.RootId) || isRoom)))
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

                        await socketManager.DeleteFolder(folder, action: async () => await ProviderDao.RemoveProviderInfoAsync(folder.ProviderId));

                        if (isRoom)
                        {
                            await notifyClient.SendRoomRemovedAsync(folder, aces, authContext.CurrentAccount.ID);
                        }
                        
                        if (isNeedSendActions)
                        {
                            await filesMessageService.SendAsync(isRoom ? MessageAction.RoomDeleted : MessageAction.ThirdPartyDeleted, folder, _headers, 
                                folder.Id.ToString(), folder.ProviderKey);
                        }
                    }

                    ProcessedFolder(folderId);
                }
                else
                {
                    var immediately = _immediately || !FolderDao.UseTrashForRemoveAsync(folder);
                    if (immediately && FolderDao.UseRecursiveOperation(folder.Id, default(T)))
                    {
                        var files = await FileDao.GetFilesAsync(folder.Id).ToListAsync();
                        await DeleteFilesAsync(files, scope, checkPermissions: checkPermissions);

                        var folders = await FolderDao.GetFoldersAsync(folder.Id).ToListAsync();
                        await DeleteFoldersAsync(folders.Select(f => f.Id).ToList(), scope, checkPermissions: checkPermissions);

                        if (await FolderDao.IsEmptyAsync(folder.Id))
                        {
                            var aces = new List<AceWrapper>();

                            if (isRoom)
                            {
                                await roomLogoManager.DeleteAsync(folder.Id, checkPermissions);
                                aces = await fileSharing.GetSharedInfoAsync(folder);
                            }

                            await socketManager.DeleteFolder(folder, action: async () => await FolderDao.DeleteFolderAsync(folder.Id));

                            if (isRoom)
                            {
                                await notifyClient.SendRoomRemovedAsync(folder, aces, authContext.CurrentAccount.ID);
                                await filesMessageService.SendAsync(MessageAction.RoomDeleted, folder, _headers, folder.Title);
                            }
                            else
                            {
                                await filesMessageService.SendAsync(MessageAction.FolderDeleted, folder, _headers, folder.Title);
                            }

                            ProcessedFolder(folderId);
                        }
                    }
                    else
                    {
                        var files = await FileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true).ToListAsync();
                        var (isError, message) = await WithErrorAsync(scope, files, true, checkPermissions);
                        if (!_ignoreException && isError)
                        {
                            this[Err] = message;
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

                                await socketManager.DeleteFolder(folder, action: async () => await FolderDao.DeleteFolderAsync(folder.Id));

                                if (isNeedSendActions)
                                {
                                    if (isRoom)
                                    {
                                        await notifyClient.SendRoomRemovedAsync(folder, aces, authContext.CurrentAccount.ID);
                                        await filesMessageService.SendAsync(MessageAction.RoomDeleted, folder, _headers, folder.Title);
                                    }
                                    else
                                    {
                                        await filesMessageService.SendAsync(MessageAction.FolderDeleted, folder, _headers, folder.Title);
                                    }
                                }
                            }
                            else
                            {
                                await socketManager.DeleteFolder(folder, action: async () => await FolderDao.MoveFolderAsync(folder.Id, _trashId, CancellationToken));

                                if (isNeedSendActions)
                                {
                                    await filesMessageService.SendAsync(MessageAction.FolderMovedToTrash, folder, _headers, folder.Title);
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

    private async Task DeleteFilesAsync(IEnumerable<T> fileIds, IServiceScope scope, bool isNeedSendActions = false, bool checkPermissions = true)
    {
        var scopeClass = scope.ServiceProvider.GetService<FileDeleteOperationScope>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();

        var (fileMarker, filesMessageService, _) = scopeClass;
        foreach (var fileId in fileIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var file = await FileDao.GetFileAsync(fileId);
            var (isError, message) = await WithErrorAsync(scope, new[] { file }, false, checkPermissions);
            if (file == null)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FileNotFound;
            }
            else if (!_ignoreException && isError)
            {
                this[Err] = message;
            }
            else
            {
                await fileMarker.RemoveMarkAsNewForAllAsync(file);
                if (!_immediately && FileDao.UseTrashForRemove(file))
                {
                    await socketManager.DeleteFileAsync(file, action: async () => await FileDao.MoveFileAsync(file.Id, _trashId, file.RootFolderType == FolderType.USER));
                    
                    if (isNeedSendActions)
                    {
                        await filesMessageService.SendAsync(MessageAction.FileMovedToTrash, file, _headers, file.Title);
                    }

                    if (file.ThumbnailStatus == Thumbnail.Waiting)
                    {
                        file.ThumbnailStatus = Thumbnail.NotRequired;
                        await FileDao.SetThumbnailStatusAsync(file, Thumbnail.NotRequired);
                    }
                }
                else
                {
                    try
                    {
                        await socketManager.DeleteFileAsync(file, action: async () => await FileDao.DeleteFileAsync(file.Id, file.GetFileQuotaOwner()));
                        
                        var folderDao = scope.ServiceProvider.GetService<IFolderDao<int>>();
                        
                        if (file.RootFolderType == FolderType.Archive)
                        {
                            var archiveId = await folderDao.GetFolderIDArchive(false);
                            var virtualRoomsId = await folderDao.GetFolderIDVirtualRooms(false);

                            await folderDao.ChangeTreeFolderSizeAsync(archiveId, (-1) * file.ContentLength);

                        }
                        else if (file.RootFolderType == FolderType.TRASH)
                        {
                            await folderDao.ChangeTreeFolderSizeAsync(_trashId, (-1) * file.ContentLength);
                        }
                        
                        if (_headers != null)
                        {
                            if (isNeedSendActions)
                            {
                                await filesMessageService.SendAsync(MessageAction.FileDeleted, file, _headers, file.Title);
                            }
                        }
                        else
                        {
                            await filesMessageService.SendAsync(MessageAction.FileDeleted, file, MessageInitiator.AutoCleanUp, file.Title);
                        }
                    }
                    catch (Exception ex)
                    {
                        this[Err] = ex.Message;
                        Logger.ErrorWithException(ex);
                    }

                    await LinkDao.DeleteAllLinkAsync(file.Id.ToString());
                    await FileDao.SaveProperties(file.Id, null);
                }

                ProcessedFile(fileId);
            }

            await ProgressStep(fileId: FolderDao.CanCalculateSubitems(fileId) ? default : fileId);
        }
    }

    private async Task<(bool isError, string message)> WithErrorAsync(IServiceScope scope, IEnumerable<File<T>> files, bool folder, bool checkPermissions)
    {
        var entryManager = scope.ServiceProvider.GetService<EntryManager>();
        var fileTracker = scope.ServiceProvider.GetService<FileTrackerHelper>();

        foreach (var file in files)
        {
            string error;
            if (checkPermissions && !await FilesSecurity.CanDeleteAsync(file))
            {
                error = FilesCommonResource.ErrorMessage_SecurityException_DeleteFile;

                return (true, error);
            }
            if (checkPermissions && await entryManager.FileLockedForMeAsync(file.Id))
            {
                error = FilesCommonResource.ErrorMessage_LockedFile;

                return (true, error);
            }
            if (fileTracker.IsEditing(file.Id))
            {
                error = folder ? FilesCommonResource.ErrorMessage_SecurityException_DeleteEditingFolder : FilesCommonResource.ErrorMessage_SecurityException_DeleteEditingFile;

                return (true, error);
            }
        }

        return (false, null);
    }
}

[Scope]
public record FileDeleteOperationScope(
    FileMarker FileMarker, 
    FilesMessageService FilesMessageService, 
    RoomLogoManager RoomLogoManager);
