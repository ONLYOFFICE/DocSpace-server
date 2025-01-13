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
        FilesVersions = versions;
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
    private readonly Dictionary<string, StringValues> _headers;
    private readonly IEnumerable<int> _filesVersions;

    public FileDeleteOperation(IServiceProvider serviceProvider, FileDeleteOperationData<T> fileOperationData)
    : base(serviceProvider, fileOperationData)
    {
        _ignoreException = fileOperationData.IgnoreException;
        _immediately = fileOperationData.Immediately;
        _headers = fileOperationData.Headers?.ToDictionary(x => x.Key, x => new StringValues(x.Value));
        _isEmptyTrash = fileOperationData.IsEmptyTrash;
        _filesVersions = fileOperationData.FilesVersions;
        this[OpType] = (int)FileOperationType.Delete;
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
            this[Res] += $"folder_{root.Id}{SplitChar}";
        }

        if (_filesVersions != null && _filesVersions.Any() && Files.Count > 0)
        {
            await DeleteFileVersionAsync(Files.FirstOrDefault(), _filesVersions, serviceScope);
        }
        else
        {
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
                && !DocSpaceHelper.IsRoom(folder.FolderType)
                && (folder.FolderType is FolderType.InProcessFormFolder or FolderType.ReadyFormFolder && folder.RootFolderType != FolderType.Archive))
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
                        
                        if (folder.FolderType is FolderType.FormFillingFolderInProgress or FolderType.FormFillingFolderDone)
                        {
                            await FolderDao.ChangeFolderTypeAsync(folder, FolderType.DEFAULT);
                            foreach (var file in files)
                            {
                                await LinkDao.DeleteAllLinkAsync(file.Id);
                                await FileDao.SaveProperties(file.Id, null);
                            }
                        }

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
            var (isError, message) = await WithErrorAsync(scope, [file], false, checkPermissions);
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
                await LinkDao.DeleteAllLinkAsync(file.Id);
                await FileDao.SaveProperties(file.Id, null);

                if (!_immediately && FileDao.UseTrashForRemove(file))
                {
                    try
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
                    catch (Exception ex)
                    {
                        this[Err] = ex.Message;
                        Logger.ErrorWithException(ex);
                    }
                    
                }
                else
                {
                    try
                    {
                        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
                        var tagDao = daoFactory.GetTagDao<T>();
                        var fromRoomTags = tagDao.GetTagsAsync(fileId, FileEntryType.File, TagType.FromRoom);
                        var fromRoomTag = await fromRoomTags.FirstOrDefaultAsync();
                        
                        await socketManager.DeleteFileAsync(file, action: async () => await FileDao.DeleteFileAsync(file.Id, fromRoomTag == null ? file.GetFileQuotaOwner() : ASC.Core.Configuration.Constants.CoreSystem.ID));
                        
                        var folderDao = scope.ServiceProvider.GetService<IFolderDao<int>>();
                        
                        if (file.RootFolderType == FolderType.Archive)
                        {
                            var archiveId = await folderDao.GetFolderIDArchive(false);
                            await folderDao.ChangeTreeFolderSizeAsync(archiveId, (-1) * file.ContentLength);

                        }
                        else if (file.RootFolderType == FolderType.TRASH)
                        {
                            await folderDao.ChangeTreeFolderSizeAsync(_trashId, (-1) * file.ContentLength);
                        }
                        
                        if (_headers is { Count: > 0 })
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
                }

                ProcessedFile(fileId);
            }

            await ProgressStep(fileId: FolderDao.CanCalculateSubitems(fileId) ? default : fileId);
        }
    }
    
    private async Task DeleteFileVersionAsync(T fileId, IEnumerable<int> versions, IServiceScope scope)
    {
        var socketManager = scope.ServiceProvider.GetService<SocketManager>();
        var filesMessageService = scope.ServiceProvider.GetService<FilesMessageService>();
        
        var file = await FileDao.GetFileAsync(fileId);
        
        if (file == null)
        {
            this[Err] = FilesCommonResource.ErrorMessage_FileNotFound;
        } else if (file.RootFolderType is FolderType.Archive or FolderType.TRASH)
        {
            this[Err] = FilesCommonResource.ErrorMessage_SecurityException;
        }
        else
        {
            foreach (var v in versions)
            {
                CancellationToken.ThrowIfCancellationRequested();

                var (isError, message) = await WithErrorAsync(scope, [file], false, true);

                if (file.Version == v)
                {
                    this[Err] = FilesCommonResource.ErrorMessage_SecurityException_FileVersion;
                }
                else if (!_ignoreException && isError)
                {
                    this[Err] = message;
                }
                else
                {
                    try
                    {
                        await FileDao.DeleteFileVersionAsync(file, v);
                        await socketManager.UpdateFileAsync(file);

                        if (_headers is { Count: > 0 })
                        {
                            await filesMessageService.SendAsync(MessageAction.FileVersionRemoved, file, _headers, file.Title, file.Version.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        this[Err] = ex.Message;
                        Logger.ErrorWithException(ex);
                    }

                    this[Process]++;
                    this[Res] += $"file_{fileId}{SplitChar}";
                }

                await ProgressStep();
            }
        }
    }

    private async Task<(bool isError, string message)> WithErrorAsync(IServiceScope scope, IEnumerable<File<T>> files, bool folder, bool checkPermissions)
    {
        var lockerManager = scope.ServiceProvider.GetService<LockerManager>();
        var fileTracker = scope.ServiceProvider.GetService<FileTrackerHelper>();

        foreach (var file in files)
        {
            string error;
            if (checkPermissions && !await FilesSecurity.CanDeleteAsync(file))
            {
                error = FilesCommonResource.ErrorMessage_SecurityException_DeleteFile;

                return (true, error);
            }
            if (checkPermissions && await lockerManager.FileLockedForMeAsync(file.Id))
            {
                error = FilesCommonResource.ErrorMessage_LockedFile;

                return (true, error);
            }
            if (await fileTracker.IsEditingAsync(file.Id))
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
