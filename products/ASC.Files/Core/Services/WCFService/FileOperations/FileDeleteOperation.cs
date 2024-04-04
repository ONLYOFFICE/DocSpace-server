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
    public bool Immediately { get; set; }
    
    [ProtoMember(8)]
    public bool IsEmptyTrash { get; set; }

    [ProtoMember(9)]
    public bool HiddenOperation { get; set; }

    public FileDeleteOperationData()
    {
        
    }

    public FileDeleteOperationData(IEnumerable<T> folders,
        IEnumerable<T> files,
        int tenantId,
        IDictionary<string, string> headers,
        ExternalSessionSnapshot sessionSnapshot,
        bool holdResult = true,
        bool immediately = false,
        bool isEmptyTrash = false,
        bool hiddenOperation = false) : base(folders, files, tenantId, headers, sessionSnapshot, holdResult)
    {
        Immediately = immediately;
        IsEmptyTrash = isEmptyTrash;
        HiddenOperation = hiddenOperation;
    }
}

[Transient]
public class FileDeleteOperation(IServiceProvider serviceProvider) : ComposeFileOperation<FileDeleteOperationData<string>, FileDeleteOperationData<int>>(serviceProvider)
{
    protected override FileOperationType FileOperationType { get => FileOperationType.Delete; }

    public void Init(bool holdResult, bool hiddenOperation)
    {
        base.Init(holdResult);

        this[Hidden] = hiddenOperation;
    }

    public override void Init(FileDeleteOperationData<int> data, FileDeleteOperationData<string> thirdPartyData, string taskId)
    {
        base.Init(data, thirdPartyData, taskId);

        this[Hidden] = data?.HiddenOperation ?? false;
    }

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
    private readonly bool _immediately;
    private readonly bool _isEmptyTrash;
    private readonly bool _hiddenOperation;

    public FileDeleteOperation(IServiceProvider serviceProvider, FileDeleteOperationData<T> fileOperationData)
    : base(serviceProvider, fileOperationData)
    {
        _immediately = fileOperationData.Immediately;
        _isEmptyTrash = fileOperationData.IsEmptyTrash;
        _hiddenOperation = fileOperationData.HiddenOperation;
        this[OpType] = (int)FileOperationType.Delete;
        this[Hidden] = _hiddenOperation;
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

        var canMarkAsRemoved = false;
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
            canMarkAsRemoved = FolderDao.CanMarkFolderAsRemoved(root.Id);
        }

        if (!_hiddenOperation && _immediately && canMarkAsRemoved)
        {
            var fileOperationsManager = serviceScope.ServiceProvider.GetService<FileOperationsManager>();
            var socketManager = serviceScope.ServiceProvider.GetService<SocketManager>();

            await MarkFilesAsRemovedAsync(socketManager, Files);
            await MarkFoldersAsRemovedAsync(socketManager, Folders);

            var headers = Headers.ToDictionary(x => x.Key, x => x.Value.ToString());

            await fileOperationsManager.PublishHiddenDelete(Folders, Files, _isEmptyTrash, headers);

            if (_isEmptyTrash)
            {
                var trash = await folderDao.GetFolderAsync(_trashId);
                await filesMessageService.SendAsync(MessageAction.TrashEmptied, trash, Headers);
            }

            return;
        }

        await DeleteFilesAsync(Files, serviceScope, isNeedSendActions: !_isEmptyTrash);
        await DeleteFoldersAsync(Folders, serviceScope, isNeedSendActions: !_isEmptyTrash);
    }

    private async Task MarkFilesAsRemovedAsync(SocketManager socketManager, IEnumerable<T> filesIds)
    {
        if (!filesIds.Any())
        {
            return;
        }

        await FileDao.MarkFilesAsRemovedAsync(filesIds);

        await foreach (var file in FileDao.GetFilesAsync(filesIds))
        {
            await socketManager.DeleteFileAsync(file);
        }
    }

    private async Task MarkFoldersAsRemovedAsync(SocketManager socketManager, IEnumerable<T> folderIds)
    {
        if (!folderIds.Any())
        {
            return;
        }

        await FolderDao.MarkFoldersAsRemovedAsync(folderIds);

        foreach (var folderId in folderIds)
        {
            var folder = await FolderDao.GetFolderAsync(folderId, true);

            await socketManager.DeleteFolder(folder);

            if (folder.RootFolderType != FolderType.TRASH)
            {
                await MarkFolderContentAsRemovedAsync(socketManager, folder);
            }
        }
    }

    private async Task MarkFolderContentAsRemovedAsync(SocketManager socketManager, Folder<T> folder)
    {
        var filesIds = await FileDao.GetFilesAsync(folder.Id).ToListAsync();

        await MarkFilesAsRemovedAsync(socketManager, filesIds);

        var subfolderIds = await FolderDao.GetFoldersAsync(folder.Id).Select(x => x.Id).ToListAsync();

        await MarkFoldersAsRemovedAsync(socketManager, subfolderIds);
    }


    private async Task DeleteFoldersAsync(IEnumerable<T> folderIds, IServiceScope scope, bool isNeedSendActions = false)
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

            var folder = await FolderDao.GetFolderAsync(folderId, true);

            T canCalculate = default;
            if (folder == null)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FolderNotFound;
            }
            else
            {
                canCalculate = FolderDao.CanCalculateSubitems(folderId) ? default : folderId;

                var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);

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
                                await roomLogoManager.DeleteAsync(providerInfo.FolderId, checkPermissions: false);
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
                            await filesMessageService.SendAsync(isRoom ? MessageAction.RoomDeleted : MessageAction.ThirdPartyDeleted, folder, Headers, 
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
                        var files = await FileDao.GetFilesAsync(folder.Id, true).ToListAsync();
                        await DeleteFilesAsync(files, scope);

                        var folders = await FolderDao.GetFoldersAsync(folder.Id, true).ToListAsync();
                        await DeleteFoldersAsync(folders.Select(f => f.Id).ToList(), scope);

                        if (await FolderDao.IsEmptyAsync(folder.Id))
                        {
                            var aces = new List<AceWrapper>();

                            if (isRoom)
                            {
                                await roomLogoManager.DeleteAsync(folder.Id, checkPermissions: false);
                                aces = await fileSharing.GetSharedInfoAsync(folder);
                            }

                            if (_hiddenOperation)
                            {
                                await FolderDao.DeleteFolderAsync(folder.Id);
                            }
                            else
                            {
                                await socketManager.DeleteFolder(folder, action: async () => await FolderDao.DeleteFolderAsync(folder.Id));
                            }

                            if (isRoom)
                            {
                                await notifyClient.SendRoomRemovedAsync(folder, aces, authContext.CurrentAccount.ID);
                                await filesMessageService.SendAsync(MessageAction.RoomDeleted, folder, Headers, folder.Title);
                            }
                            else
                            {
                                await filesMessageService.SendAsync(MessageAction.FolderDeleted, folder, Headers, folder.Title);
                            }

                            ProcessedFolder(folderId);
                        }
                    }
                    else
                    {
                        var files = await FileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true).ToListAsync();

                        if (immediately)
                        {
                            var aces = new List<AceWrapper>();

                            if (isRoom)
                            {
                                var room = await roomLogoManager.DeleteAsync(folder.Id, checkPermissions: false);
                                await socketManager.UpdateFolderAsync(room);
                                aces = await fileSharing.GetSharedInfoAsync(folder);
                            }

                            if (_hiddenOperation)
                            {
                                await FolderDao.DeleteFolderAsync(folder.Id);
                            }
                            else
                            {
                                await socketManager.DeleteFolder(folder, action: async () => await FolderDao.DeleteFolderAsync(folder.Id));
                            }

                            if (isNeedSendActions)
                            {
                                if (isRoom)
                                {
                                    await notifyClient.SendRoomRemovedAsync(folder, aces, authContext.CurrentAccount.ID);
                                    await filesMessageService.SendAsync(MessageAction.RoomDeleted, folder, Headers, folder.Title);
                                }
                                else
                                {
                                    await filesMessageService.SendAsync(MessageAction.FolderDeleted, folder, Headers, folder.Title);
                                }
                            }
                        }
                        else
                        {
                            await socketManager.DeleteFolder(folder, action: async () => await FolderDao.MoveFolderAsync(folder.Id, _trashId, CancellationToken));

                            if (isNeedSendActions)
                            {
                                await filesMessageService.SendAsync(MessageAction.FolderMovedToTrash, folder, Headers, folder.Title);
                            }
                        }

                        ProcessedFolder(folderId);
                    }
                }
            }
            await ProgressStep(canCalculate);
        }
    }

    private async Task DeleteFilesAsync(IEnumerable<T> fileIds, IServiceScope scope, bool isNeedSendActions = false)
    {
        var scopeClass = scope.ServiceProvider.GetService<FileDeleteOperationScope>();
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();

        var (fileMarker, filesMessageService, _) = scopeClass;
        foreach (var fileId in fileIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var file = await FileDao.GetFileAsync(fileId, true);
            if (file == null)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FileNotFound;
            }
            else
            {
                await fileMarker.RemoveMarkAsNewForAllAsync(file);
                if (!_immediately && FileDao.UseTrashForRemove(file))
                {
                    await socketManager.DeleteFileAsync(file, action: async () => await FileDao.MoveFileAsync(file.Id, _trashId, file.RootFolderType == FolderType.USER));
                    
                    if (isNeedSendActions)
                    {
                        await filesMessageService.SendAsync(MessageAction.FileMovedToTrash, file, Headers, file.Title);
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
                        if (_hiddenOperation)
                        {
                            await FileDao.DeleteFileAsync(file.Id, file.GetFileQuotaOwner());
                        }
                        else
                        {
                            await socketManager.DeleteFileAsync(file, action: async () => await FileDao.DeleteFileAsync(file.Id, file.GetFileQuotaOwner()));
                        }

                        var folderDao = scope.ServiceProvider.GetService<IFolderDao<int>>();
                        
                        if (file.RootFolderType == FolderType.Archive)
                        {
                            var archiveId = await folderDao.GetFolderIDArchive(false);
                            var virtualRoomsId = await folderDao.GetFolderIDVirtualRooms(false);

                            await folderDao.ChangeTreeFolderSizeAsync(archiveId, (-1) * file.ContentLength);
                            await folderDao.ChangeTreeFolderSizeAsync(virtualRoomsId, file.ContentLength);

                        }
                        else if (file.RootFolderType == FolderType.TRASH)
                        {
                            await folderDao.ChangeTreeFolderSizeAsync(_trashId, (-1) * file.ContentLength);
                        }
                        
                        if (Headers != null)
                        {
                            if (isNeedSendActions)
                            {
                                await filesMessageService.SendAsync(MessageAction.FileDeleted, file, Headers, file.Title);
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
}

[Scope]
public record FileDeleteOperationScope(
    FileMarker FileMarker, 
    FilesMessageService FilesMessageService, 
    RoomLogoManager RoomLogoManager);
