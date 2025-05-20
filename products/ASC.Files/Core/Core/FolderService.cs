// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Core.Core;

/// <summary>
/// Represents a service responsible for handling operations related to folders and rooms within the system.
/// </summary>
/// <remarks>
/// This service provides methods for creating, retrieving, renaming, and managing folders and rooms.
/// It integrates various application components to facilitate folder-related workflows and ensure secure and organized data operations.
/// </remarks>
[Scope]
public class FolderService(
    GlobalFolderHelper globalFolderHelper,
    AuthContext authContext,
    UserManager userManager,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    FilesMessageService filesMessageService,
    NotifyClient notifyClient,
    IServiceProvider serviceProvider,
    TenantManager tenantManager,
    EntryStatusManager entryStatusManager,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
    QuotaSocketManager quotaSocketManager,
    RoomLogoManager roomLogoManager,
    WatermarkManager watermarkManager,
    CustomTagsService customTagsService,
    WebhookManager webhookManager,
    ILogger<FolderService> logger,
    FilesSettingsHelper filesSettingsHelper,
    BreadCrumbsManager breadCrumbsManager,
    FileSharing fileSharing,
    FileMarker fileMarker,
    FileConverter fileConverter,
    FileMarkAsReadOperationsManager fileOperationsManager)
{
    /// <summary>
    /// Retrieves a folder asynchronously by its identifier with additional metadata such as tags, favorites, and pin information.
    /// </summary>
    /// <typeparam name="T">The type of the folder identifier.</typeparam>
    /// <param name="folderId">The identifier of the folder to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the folder with the specified identifier, including additional metadata.</returns>
    public async Task<Folder<T>> GetFolderAsync<T>(T folderId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var tagDao = daoFactory.GetTagDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);

        if (folder == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanReadAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }

        var tag = await tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, folder).FirstOrDefaultAsync();
        if (tag != null)
        {
            folder.NewForMe = tag.Count;
        }

        var tags = await tagDao.GetTagsAsync(folder.Id, FileEntryType.Folder, null, authContext.CurrentAccount.ID).ToListAsync();
        folder.Pinned = tags.Any(r => r.Type == TagType.Pin);
        folder.IsFavorite = tags.Any(r => r.Type == TagType.Favorite);
        folder.Tags = await tagDao.GetTagsAsync(folder.Id, FileEntryType.Folder, TagType.Custom).ToListAsync();

        return folder;
    }

    /// <summary>
    /// Retrieves all subfolders within a specified parent folder asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the parent folder identifier.</typeparam>
    /// <param name="parentId">The identifier of the parent folder whose subfolders are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of file entries representing the subfolders.</returns>
    public async Task<IEnumerable<FileEntry>> GetFoldersAsync<T>(T parentId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        IEnumerable<FileEntry> entries;

        try
        {
            var parent = await folderDao.GetFolderAsync(parentId);

            Folder<T> parentRoom = null;

            if (parent.RootFolderType == FolderType.VirtualRooms)
            {
                parentRoom = !DocSpaceHelper.IsRoom(parent.FolderType) && parent.FolderType != FolderType.VirtualRooms ? await folderDao.GetFirstParentTypeFromFileEntryAsync(parent) : parent;
            }

            (entries, _) = await entryManager.GetEntriesAsync(parent, parentRoom, 0, -1, [FilterType.FoldersOnly], false, Guid.Empty, string.Empty, [], false, false, new OrderBy(SortedByType.AZ, true));
        }
        catch (Exception e)
        {
            throw FileStorageService.GenerateException(e, logger, authContext);
        }

        return entries;
    }


    /// <summary>
    /// Creates a new folder asynchronously under the specified parent folder with the given title.
    /// </summary>
    /// <typeparam name="T">The type of the folder identifier.</typeparam>
    /// <param name="parentId">The identifier of the parent folder where the new folder will be created.</param>
    /// <param name="title">The title of the folder to be created.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the newly created folder.</returns>
    public async Task<Folder<T>> CreateFolderAsync<T>(T parentId, string title)
    {
        var folder = await InternalCreateFolderAsync(parentId, title);

        await socketManager.CreateFolderAsync(folder);

        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await folderDao.GetParentFoldersAsync(folder.Id).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));
        if (room != null && !DocSpaceHelper.FormsFillingSystemFolders.Contains(folder.FolderType))
        {
            var whoCanRead = await fileSecurity.WhoCanReadAsync(room, true);
            await notifyClient.SendFolderCreatedInRoom(room, whoCanRead, folder, authContext.CurrentAccount.ID);
        }

        await filesMessageService.SendAsync(MessageAction.FolderCreated, folder, folder.Title);

        await webhookManager.PublishAsync(WebhookTrigger.FolderCreated, folder);

        return folder;
    }

    /// <summary>
    /// Renames a folder asynchronously, updating its title while verifying user permissions and managing associated metadata.
    /// </summary>
    /// <typeparam name="T">The type of the folder identifier.</typeparam>
    /// <param name="folderId">The identifier of the folder to rename.</param>
    /// <param name="title">The new title for the folder.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated folder with the new title.</returns>
    public async Task<Folder<T>> FolderRenameAsync<T>(T folderId, string title)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);
        if (folder == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        var canEdit = DocSpaceHelper.IsRoom(folder.FolderType)
            ? folder.RootFolderType != FolderType.Archive && await fileSecurity.CanEditRoomAsync(folder)
            : await fileSecurity.CanRenameAsync(folder);

        if (!canEdit)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_RenameFolder);
        }

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_RenameFolder);
        }

        if (folder.RootFolderType == FolderType.TRASH)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        if (folder.RootFolderType == FolderType.Archive)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UpdateArchivedRoom);
        }

        var folderAccess = folder.Access;
        var renamedFolder = folder;

        if (!string.Equals(folder.Title, title, StringComparison.Ordinal))
        {
            var oldTitle = folder.Title;
            T newFolderId = default;

            if (folder.MutableId)
            {
                await socketManager.DeleteFolder(folder, action: async () =>
                {
                    newFolderId = await folderDao.RenameFolderAsync(folder, title);
                });
            }
            else
            {
                newFolderId = await folderDao.RenameFolderAsync(folder, title);
            }

            renamedFolder = await folderDao.GetFolderAsync(newFolderId);
            renamedFolder.Access = folderAccess;

            if (DocSpaceHelper.IsRoom(renamedFolder.FolderType))
            {
                await filesMessageService.SendAsync(MessageAction.RoomRenamed, oldTitle, renamedFolder, renamedFolder.Title);

                await webhookManager.PublishAsync(WebhookTrigger.RoomUpdated, renamedFolder);
            }
            else
            {
                await filesMessageService.SendAsync(MessageAction.FolderRenamed, renamedFolder, renamedFolder.Title, oldTitle);

                await webhookManager.PublishAsync(WebhookTrigger.FolderUpdated, renamedFolder);
            }

            //if (!folder.ProviderEntry)
            //{
            //    FoldersIndexer.IndexAsync(FoldersWrapper.GetFolderWrapper(ServiceProvider, folder));
            //}
        }

        var newTags = tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, renamedFolder);
        var tag = await newTags.FirstOrDefaultAsync();
        if (tag != null)
        {
            renamedFolder.NewForMe = tag.Count;
        }

        if (renamedFolder.RootFolderType == FolderType.USER
            && !Equals(renamedFolder.RootCreateBy, authContext.CurrentAccount.ID)
            && !await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(renamedFolder.ParentId)))
        {
            renamedFolder.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
        }

        await entryStatusManager.SetIsFavoriteFolderAsync(renamedFolder);

        if (renamedFolder.MutableId)
        {
            await socketManager.CreateFolderAsync(renamedFolder);
        }
        else
        {
            await socketManager.UpdateFolderAsync(renamedFolder);
        }

        return renamedFolder;
    }
    
    public async Task<DataWrapper<T>> GetFolderItemsAsync<T>(
        T parentId,
        int from,
        int count,
        IEnumerable<FilterType> filterTypes,
        bool subjectGroup,
        string subject,
        string searchText,
        string[] extension,
        bool searchInContent,
        bool withSubfolders,
        OrderBy orderBy,
        SearchArea searchArea = SearchArea.Active,
        T roomId = default,
        bool withoutTags = false,
        IEnumerable<string> tagNames = null,
        bool excludeSubject = false,
        ProviderFilter provider = ProviderFilter.None,
        SubjectFilter subjectFilter = SubjectFilter.Owner,
        ApplyFilterOption applyFilterOption = ApplyFilterOption.All,
        QuotaFilter quotaFilter = QuotaFilter.All,
        StorageFilter storageFilter = StorageFilter.None,
        FormsItemDto formsItemDto = null)
    {
        var subjectId = string.IsNullOrEmpty(subject) ? Guid.Empty : new Guid(subject);

        var folderDao = daoFactory.GetFolderDao<T>();

        Folder<T> parent = null;
        Folder<T> parentRoom = null;

        try
        {
            parent = await folderDao.GetFolderAsync(parentId);

            if (parent == null)
            {
                throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
            }

            if (parent != null && !string.IsNullOrEmpty(parent.Error))
            {
                throw new Exception(parent.Error);
            }

            if (parent == null)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
            }

            if (parent.RootFolderType == FolderType.VirtualRooms)
            {
                parentRoom = !DocSpaceHelper.IsRoom(parent.FolderType) && parent.FolderType != FolderType.VirtualRooms && !parent.ProviderEntry ? await folderDao.GetFirstParentTypeFromFileEntryAsync(parent) : parent;

                parent.ParentRoomType = parentRoom.FolderType;
            }

            if (parent.RootFolderType == FolderType.RoomTemplates)
            {
                parentRoom = !DocSpaceHelper.IsRoom(parent.FolderType) && parent.FolderType != FolderType.RoomTemplates && !parent.ProviderEntry ? await folderDao.GetFirstParentTypeFromFileEntryAsync(parent) : parent;

                parent.ParentRoomType = parentRoom.FolderType;
            }
        }
        catch (Exception e)
        {
            if (parent is { ProviderEntry: true })
            {
                throw FileStorageService.GenerateException(new Exception(FilesCommonResource.ErrorMessage_SharpBoxException, e), logger, authContext);
            }

            throw FileStorageService.GenerateException(e, logger, authContext);
        }

        if (!await fileSecurity.CanReadAsync(parent))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
        }

        if (parent.RootFolderType == FolderType.TRASH && !Equals(parent.Id, await globalFolderHelper.FolderTrashAsync))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        if (parent.FolderType is FolderType.FormFillingFolderDone or FolderType.FormFillingFolderInProgress)
        {
            if (parent.ShareRecord is { Share: FileShare.FillForms })
            {
                subjectId = authContext.CurrentAccount.ID;
            }
        }

        if (orderBy != null)
        {
            await filesSettingsHelper.SetDefaultOrder(orderBy);
        }
        else
        {
            orderBy = await filesSettingsHelper.GetDefaultOrder();
        }

        if (Equals(parent.Id, await globalFolderHelper.FolderShareAsync) && orderBy.SortedBy == SortedByType.DateAndTime)
        {
            orderBy.SortedBy = SortedByType.New;
        }

        searchArea = parent.FolderType switch
        {
            FolderType.Archive => SearchArea.Archive,
            FolderType.RoomTemplates => SearchArea.Templates,
            _ => searchArea
        };

        int total;
        IEnumerable<FileEntry> entries;

        try
        {
            (entries, total) = await entryManager.GetEntriesAsync(
                parent,
                parentRoom,
                from,
                count,
                filterTypes,
                subjectGroup,
                subjectId,
                searchText,
                extension,
                searchInContent,
                withSubfolders,
                orderBy,
                roomId,
                searchArea,
                withoutTags,
                tagNames,
                excludeSubject,
                provider,
                subjectFilter,
                applyFilterOption,
                quotaFilter,
                storageFilter,
                formsItemDto);
        }
        catch (Exception e)
        {
            if (parent.ProviderEntry)
            {            
                throw FileStorageService.GenerateException(new Exception(FilesCommonResource.ErrorMessage_SharpBoxException, e), logger, authContext);
            }

            throw FileStorageService.GenerateException(e, logger, authContext);
        }

        var breadCrumbsTask = breadCrumbsManager.GetBreadCrumbsAsync(parentId, folderDao);
        var shareableTask = fileSharing.CanSetAccessAsync(parent);
        var newTask = fileMarker.GetRootFoldersIdMarkedAsNewAsync(parentId);
        var breadCrumbs = await breadCrumbsTask;

        var prevVisible = breadCrumbs.ElementAtOrDefault(breadCrumbs.Count - 2);
        if (prevVisible != null && !DocSpaceHelper.IsRoom(parent.FolderType) && prevVisible.FileEntryType == FileEntryType.Folder)
        {
            if (prevVisible is Folder<string> f1)
            {
                parent.ParentId = (T)Convert.ChangeType(f1.Id, typeof(T));
            }
            else if (prevVisible is Folder<int> f2)
            {
                parent.ParentId = (T)Convert.ChangeType(f2.Id, typeof(T));
            }
        }

        parent.Shareable =
            parent.FolderType == FolderType.SHARE ||
            parent.RootFolderType == FolderType.Privacy ||
            await shareableTask;

        entries = entries.ToAsyncEnumerable().WhereAwait(async x =>
        {
            if (x.FileEntryType == FileEntryType.Folder)
            {
                return true;
            }

            if (x is File<string> f1)
            {
                return !await fileConverter.IsConverting(f1);
            }

            return x is File<int> f2 && !await fileConverter.IsConverting(f2);
        }).ToEnumerable();

        if (parent.FolderType == FolderType.Recent && searchArea == SearchArea.RecentByLinks)
        {
            parent.Title = FilesUCResource.MyFiles;
        }

        var result = new DataWrapper<T>
        {
            Total = total,
            Entries = entries.ToList(),
            FolderPathParts =
            [
                ..breadCrumbs.Select(object (f) =>
                {
                    if (f.FileEntryType == FileEntryType.Folder)
                    {
                        switch (f)
                        {
                            case Folder<string> f1:
                                return new { f1.Id, f1.Title, RoomType = DocSpaceHelper.MapToRoomType(f1.FolderType) };
                            case Folder<int> f2:
                                {
                                    var title = f2.FolderType is FolderType.Recent && searchArea == SearchArea.RecentByLinks
                                        ? FilesUCResource.MyFiles
                                        : f2.Title;

                                    return new { f2.Id, title, RoomType = DocSpaceHelper.MapToRoomType(f2.FolderType) };
                                }
                        }
                    }

                    return 0;
                })
            ],
            FolderInfo = parent,
            New = await newTask,
            ParentRoom = parentRoom
        };

        return result;
    }
    
    public async Task<List<FileEntry>> GetNewItemsAsync<T>(T folderId)
    {
        try
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var folder = await folderDao.GetFolderAsync(folderId);

            var result = await fileMarker.MarkedItemsAsync(folder).Where(e => e.FileEntryType == FileEntryType.File).ToListAsync();

            result = [..await entryManager.SortEntries<T>(result, new OrderBy(SortedByType.DateAndTime, false))];

            if (result.Count == 0)
            {
                await fileOperationsManager.Publish([JsonSerializer.SerializeToElement(folderId)], []);
            }

            return result;
        }
        catch (Exception e)
        {
            throw FileStorageService.GenerateException(e, logger, authContext);
        }
    }
    
    public async Task<(List<int>, List<int>)> GetTrashContentAsync()
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var trashId = await folderDao.GetFolderIDTrashAsync(true);
        var foldersIdTask = await folderDao.GetFoldersAsync(trashId).Select(f => f.Id).ToListAsync();
        var filesIdTask = await fileDao.GetFilesAsync(trashId).ToListAsync();

        return (foldersIdTask, filesIdTask);
    }
    
    public async Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        return await folderDao.GetFilesUsedSpace();
    }
    
    internal async Task<Folder<T>> InternalCreateFolderAsync<T>(T parentId, string title, FolderType folderType = FolderType.DEFAULT, bool privacy = false, bool? indexing = false, long? quota = TenantEntityQuotaSettings.DefaultQuotaValue, RoomDataLifetime lifetime = null, bool? denyDownload = false, WatermarkRequestDto watermark = null, string color = null, string cover = null, IEnumerable<string> names = null, LogoRequest logo = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentNullException.ThrowIfNull(parentId);

        var folderDao = daoFactory.GetFolderDao<T>();

        var parent = await folderDao.GetFolderAsync(parentId);
        var isRoom = DocSpaceHelper.IsRoom(folderType);

        if (parent == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (parent.FolderType == FolderType.RoomTemplates)
        {
            if (!await fileSecurity.CanCreateFromAsync(parent))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }
        }
        else
        {
            if (!await fileSecurity.CanCreateAsync(parent))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }
        }

        if (parent.RootFolderType == FolderType.Archive)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UpdateArchivedRoom);
        }

        if (parent.FolderType == FolderType.Archive)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (!isRoom && parent.FolderType == FolderType.VirtualRooms)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenantId);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new InvalidOperationException(Resource.RoomQuotaGreaterPortalError);
        }

        try
        {
            var newFolder = serviceProvider.GetService<Folder<T>>();
            newFolder.Title = title;
            newFolder.ParentId = parent.Id;
            newFolder.FolderType = folderType;
            newFolder.SettingsPrivate = parent.SettingsPrivate ? parent.SettingsPrivate : privacy;
            newFolder.SettingsColor = roomLogoManager.GetRandomColour();

            if (indexing.HasValue)
            {
                newFolder.SettingsIndexing = indexing.Value;
            }

            if (denyDownload.HasValue)
            {
                newFolder.SettingsDenyDownload = denyDownload.Value;
            }

            if (quota.HasValue)
            {
                newFolder.SettingsQuota = quota.Value;
            }

            newFolder.SettingsLifetime = lifetime;
            _ = RoomLogoManager.ColorChanged(color, newFolder);
            _ = await RoomLogoManager.CoverChanged(cover, newFolder);

            var folderId = await folderDao.SaveFolderAsync(newFolder);
            if (watermark != null)
            {
                try
                {
                    await watermarkManager.SetWatermarkAsync(newFolder, watermark);
                }
                catch (Exception)
                {
                }
            }


            var folder = await folderDao.GetFolderAsync(folderId);

            if (logo != null)
            {
                await roomLogoManager.SaveLogo(logo.TmpFile, logo.X, logo.Y, logo.Width, logo.Height, folder, folderDao);
            }

            var tagDao = daoFactory.GetTagDao<T>();

            if (names != null)
            {
                var tagsInfos = await tagDao.GetTagsInfoAsync(names, TagType.Custom).ToListAsync();
                var notFoundTags = names?.Where(x => tagsInfos.All(r => r.Name != x));

                if (notFoundTags != null)
                {
                    foreach (var tagInfo in notFoundTags)
                    {
                        tagsInfos.Add(await customTagsService.CreateTagAsync(tagInfo));
                    }
                }

                if (tagsInfos.Count != 0)
                {
                    var tags = tagsInfos.Select(tagInfo => Tag.Custom(Guid.Empty, folder, tagInfo.Name));

                    await tagDao.SaveTagsAsync(tags);

                    await filesMessageService.SendAsync(MessageAction.AddedRoomTags, folder, folder.Title, string.Join(',', tags.Select(x => x.Name)));
                }
            }

            if (!isRoom)
            {
                return folder;
            }

            if (folder.Id.Equals(folder.RootId))
            {
                return null;
            }

            var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);

            return folder;
        }
        catch (Exception e)
        {
            throw FileStorageService.GenerateException(e, logger, authContext);
        }
    }
}