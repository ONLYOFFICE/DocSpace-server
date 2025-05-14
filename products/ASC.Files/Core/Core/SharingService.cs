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
/// Provides methods for managing file and folder sharing within the application.
/// </summary>
/// <remarks>
/// This service handles various responsibilities such as retrieving shared information,
/// managing access control entries (ACEs), generating external and invitation links for shared resources,
/// and handling the removal of access permissions.
/// </remarks>
[Scope]
public class SharingService(
    AuthContext authContext,
    UserManager userManager,
    FilesLinkUtility filesLinkUtility,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IDaoFactory daoFactory,
    FilesMessageService filesMessageService,
    FileSharing fileSharing,
    NotifyClient notifyClient,
    IUrlShortener urlShortener,
    FileSharingAceHelper fileSharingAceHelper,
    InvitationValidator invitationValidator,
    ExternalShare externalShare,
    TenantUtil tenantUtil,
    ILogger<FolderOperationsService> logger,
    FileSecurity fileSecurity,
    EntryStatusManager entryStatusManager,
    WebhookManager webhookManager,
    FileMarker fileMarker,
    ThumbnailSettings thumbnailSettings,
    LockerManager lockerManager,
    GlobalStore globalStore,
    FileTrackerHelper fileTracker,
    IServiceProvider serviceProvider)
{
    private static readonly FrozenDictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> _roomMessageActions =
        new Dictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> { { SubjectType.InvitationLink, new Dictionary<EventType, MessageAction> { { EventType.Create, MessageAction.RoomInvitationLinkCreated }, { EventType.Update, MessageAction.RoomInvitationLinkUpdated }, { EventType.Remove, MessageAction.RoomInvitationLinkDeleted } }.ToFrozenDictionary() }, { SubjectType.ExternalLink, new Dictionary<EventType, MessageAction> { { EventType.Create, MessageAction.RoomExternalLinkCreated }, { EventType.Update, MessageAction.RoomExternalLinkUpdated }, { EventType.Remove, MessageAction.RoomExternalLinkDeleted } }.ToFrozenDictionary() } }.ToFrozenDictionary();

    private static readonly FrozenDictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> _fileMessageActions =
        new Dictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> { { SubjectType.ExternalLink, new Dictionary<EventType, MessageAction> { { EventType.Create, MessageAction.FileExternalLinkCreated }, { EventType.Update, MessageAction.FileExternalLinkUpdated }, { EventType.Remove, MessageAction.FileExternalLinkDeleted } }.ToFrozenDictionary() } }.ToFrozenDictionary();

    /// Retrieves information about shared files and folders asynchronously.
    /// <param name="fileIds">A collection of file IDs for which to retrieve shared information.</param>
    /// <param name="folderIds">A collection of folder IDs for which to retrieve shared information.</param>
    /// <typeparam name="T">The type of the identifiers for files and folders.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of AceWrapper objects detailing the shared information.</returns>
    public async Task<List<AceWrapper>> GetSharedInfoAsync<T>(
        IEnumerable<T> fileIds,
        IEnumerable<T> folderIds)
    {
        return await fileSharing.GetSharedInfoAsync(fileIds, folderIds);
    }

    /// Retrieves a filtered collection of share information for a specific file or folder asynchronously.
    /// <param name="entryId">The ID of the file or folder for which share information is to be retrieved.</param>
    /// <param name="entryType">The type of entry, either file or folder, represented by <see cref="FileEntryType"/>.</param>
    /// <param name="filterType">The type of sharing filter to apply, defined by <see cref="ShareFilterType"/>.</param>
    /// <param name="text">A search keyword to match share information.</param>
    /// <param name="offset">The starting point for retrieving records, used for pagination.</param>
    /// <param name="count">The maximum number of records to retrieve.</param>
    /// <typeparam name="T">The type of the identifier used for file or folder entries.</typeparam>
    /// <returns>An asynchronous stream of <see cref="AceWrapper"/> objects containing the filtered share information.</returns>
    public async IAsyncEnumerable<AceWrapper> GetPureSharesAsync<T>(T entryId, FileEntryType entryType, ShareFilterType filterType, string text, int offset, int count)
    {
        var entry = await GetEntryAsync(entryId, entryType);

        await foreach (var ace in fileSharing.GetPureSharesAsync(entry, filterType, null, text, offset, count))
        {
            yield return ace;
        }
    }

    /// Retrieves the primary external link for a specified file or folder entry asynchronously.
    /// <param name="entryId">The identifier of the file or folder entry for which to retrieve the primary external link.</param>
    /// <param name="entryType">Specifies whether the entry is a file or folder.</param>
    /// <param name="share">The level of file sharing permissions. Defaults to FileShare.Read.</param>
    /// <param name="title">The title of the shared link. Optional.</param>
    /// <param name="expirationDate">The expiration date for the link. Optional, defaults to no expiration.</param>
    /// <param name="denyDownload">A flag indicating whether download is denied. Defaults to false.</param>
    /// <param name="requiredAuth">A flag indicating whether authentication is required to access the link. Defaults to false.</param>
    /// <param name="password">The password associated with the shared link, if any. Optional.</param>
    /// <param name="allowUnlimitedDate">A flag allowing an unlimited expiration date for the link. Defaults to false.</param>
    /// <typeparam name="T">The type of the identifier for the file or folder entry.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result contains an AceWrapper object representing the primary external link details.</returns>
    public async Task<AceWrapper> GetPrimaryExternalLinkAsync<T>(
        T entryId,
        FileEntryType entryType,
        FileShare share = FileShare.Read,
        string title = null,
        DateTime expirationDate = default,
        bool denyDownload = false,
        bool requiredAuth = false,
        string password = null,
        bool allowUnlimitedDate = false)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        FileEntry<T> entry = entryType == FileEntryType.File
            ? await fileDao.GetFileAsync(entryId)
            : await folderDao.GetFolderAsync(entryId);

        entry.NotFoundIfNull();

        if ((entry is File<T> || entry is Folder<T> folder && !DocSpaceHelper.IsRoom(folder.FolderType)) && entry.RootFolderType == FolderType.VirtualRooms)
        {
            var room = await DocSpaceHelper.GetParentRoom(entry, folderDao);

            var linkId = await externalShare.GetLinkIdAsync();
            AceWrapper ace;

            if (linkId == Guid.Empty)
            {
                ace = await fileSharing.GetPureSharesAsync(room, ShareFilterType.PrimaryExternalLink, null, null, 0, 1).FirstOrDefaultAsync();
            }
            else
            {
                ace = await fileSharing.GetPureSharesAsync(room, [linkId]).FirstOrDefaultAsync();
            }

            if (ace == null)
            {
                throw new ItemNotFoundException();
            }

            var data = await externalShare.GetLinkDataAsync(entry, ace.Id, entryType == FileEntryType.File);
            ace.Link = await urlShortener.GetShortenLinkAsync(data.Url);

            return ace;
        }

        var link = await fileSharing.GetPureSharesAsync(entry, ShareFilterType.PrimaryExternalLink, null, null, 0, 1).FirstOrDefaultAsync();
        if (link == null)
        {
            if (entry is File<T> { IsForm: true } && share == FileShare.Read)
            {
                share = FileShare.Editing;
            }
            return await SetExternalLinkAsync(
                entry,
                Guid.NewGuid(),
                share,
                title ?? FilesCommonResource.DefaultExternalLinkTitle,
                primary: true,
                expirationDate: expirationDate != default
                    ? expirationDate
                    : entry.RootFolderType == FolderType.USER && !allowUnlimitedDate
                        ? DateTime.UtcNow.Add(filesLinkUtility.DefaultLinkLifeTime)
                        : default,
                denyDownload: denyDownload,
                requiredAuth: requiredAuth,
                password: password);
        }

        if (link.FileShareOptions.IsExpired && entry.RootFolderType == FolderType.USER && entry.FileEntryType == FileEntryType.File)
        {
            return await SetExternalLinkAsync(entry, link.Id, link.Access, FilesCommonResource.DefaultExternalLinkTitle,
                DateTime.UtcNow.Add(filesLinkUtility.DefaultLinkLifeTime), requiredAuth: link.FileShareOptions.Internal, primary: true);
        }

        return link;
    }

    /// Sets access control entries (ACE) for files and folders asynchronously.
    /// <param name="aceCollection">A collection of ACEs, along with associated files and folders, to be updated.</param>
    /// <param name="notify">A boolean value indicating whether to send notifications for the ACE changes.</param>
    /// <param name="culture">An optional culture identifier for the operation, used for localization purposes.</param>
    /// <param name="socket">A boolean value indicating whether to use socket notifications for real-time updates. Default is true.</param>
    /// <param name="beforeOwnerChange">A boolean value indicating whether the ACE update is performed prior to an ownership change. Default is false.</param>
    /// <typeparam name="T">The type of identifiers for files and folders within the ACE collection.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result contains a string indicating any warnings or results of the operation.</returns>
    public async Task<string> SetAceObjectAsync<T>(AceCollection<T> aceCollection, bool notify, string culture = null, bool socket = true, bool beforeOwnerChange = false)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        var entries = new List<FileEntry<T>>();
        string warning = null;

        foreach (var fileId in aceCollection.Files)
        {
            entries.Add(await fileDao.GetFileAsync(fileId));
        }

        foreach (var folderId in aceCollection.Folders)
        {
            entries.Add(await folderDao.GetFolderAsync(folderId));
        }

        foreach (var entry in entries)
        {
            try
            {
                var result = await fileSharingAceHelper.SetAceObjectAsync(aceCollection.Aces, entry, notify, aceCollection.Message, culture, socket, beforeOwnerChange);
                warning ??= result.Warning;

                if (!result.Changed)
                {
                    continue;
                }

                foreach (var (eventType, pastRecord, ace) in result.ProcessedItems)
                {
                    if (ace.IsLink)
                    {
                        continue;
                    }

                    switch (ace.SubjectType)
                    {
                        case SubjectType.User:
                            {
                                var user = !string.IsNullOrEmpty(ace.Email)
                                    ? await userManager.GetUserByEmailAsync(ace.Email)
                                    : await userManager.GetUsersAsync(ace.Id);

                                var name = user.DisplayUserName(false, displayUserSettingsHelper);

                                if (entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType))
                                {
                                    switch (eventType)
                                    {
                                        case EventType.Create:
                                            await filesMessageService.SendAsync(MessageAction.RoomCreateUser, entry, user.Id, ace.Access, null, true, name);
                                            await notifyClient.SendInvitedToRoom(folder, user);
                                            break;
                                        case EventType.Remove:
                                            await filesMessageService.SendAsync(MessageAction.RoomRemoveUser, entry, user.Id, name);
                                            break;
                                        case EventType.Update:
                                            await filesMessageService.SendAsync(MessageAction.RoomUpdateAccessForUser, entry, user.Id, ace.Access, pastRecord.Share, true, name);
                                            await notifyClient.SendRoomUpdateAccessForUser(folder, user, ace.Access);
                                            break;
                                    }
                                }
                                else
                                {
                                    await filesMessageService.SendAsync(
                                        entry.FileEntryType == FileEntryType.Folder ? MessageAction.FolderUpdatedAccessFor : MessageAction.FileUpdatedAccessFor, entry,
                                        entry.Title, name, FileShareExtensions.GetAccessString(ace.Access));
                                }

                                break;
                            }
                        case SubjectType.Group:
                            {
                                var group = await userManager.GetGroupInfoAsync(ace.Id);

                                if (entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType))
                                {
                                    switch (eventType)
                                    {
                                        case EventType.Create:
                                            await filesMessageService.SendAsync(MessageAction.RoomGroupAdded, entry, group.Name,
                                                FileShareExtensions.GetAccessString(ace.Access, true), group.ID.ToString());
                                            break;
                                        case EventType.Remove:
                                            await filesMessageService.SendAsync(MessageAction.RoomGroupRemove, entry, group.Name, group.ID.ToString());
                                            break;
                                        case EventType.Update:
                                            await filesMessageService.SendAsync(MessageAction.RoomUpdateAccessForGroup, entry, group.Name,
                                                FileShareExtensions.GetAccessString(ace.Access, true), group.ID.ToString(),
                                                FileShareExtensions.GetAccessString(pastRecord.Share, true));
                                            break;
                                    }
                                }

                                break;
                            }
                    }
                }
            }
            catch (Exception e)
            {
                throw GenerateException(e);
            }
        }

        return warning;
    }

    /// Creates or updates an external link for a file or folder asynchronously.
    /// <param name="entryId">The unique identifier for the file or folder.</param>
    /// <param name="entryType">The type of the entry, indicating whether it is a file or folder.</param>
    /// <param name="linkId">The unique identifier of the external link to be set or updated.</param>
    /// <param name="title">The title of the external link.</param>
    /// <param name="share">The sharing permissions to associate with the external link.</param>
    /// <param name="expirationDate">The expiration date of the external link, if any. Defaults to no expiration if not provided.</param>
    /// <param name="password">The optional password required to access the external link.</param>
    /// <param name="denyDownload">A flag indicating whether downloading is denied for the external link.</param>
    /// <param name="requiredAuth">A flag indicating whether authentication is required to use the external link.</param>
    /// <param name="primary">A flag indicating whether the external link is marked as primary.</param>
    /// <typeparam name="T">The type of the unique identifier for the file or folder.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result contains the AceWrapper object with details of the external link.</returns>
    public async Task<AceWrapper> SetExternalLinkAsync<T>(T entryId, FileEntryType entryType, Guid linkId, string title, FileShare share, DateTime expirationDate = default,
        string password = null, bool denyDownload = false, bool requiredAuth = false, bool primary = false)
    {
        FileEntry<T> entry = entryType == FileEntryType.File
            ? await daoFactory.GetFileDao<T>().GetFileAsync(entryId)
            : await daoFactory.GetFolderDao<T>().GetFolderAsync(entryId);

        return await SetExternalLinkAsync(entry.NotFoundIfNull(), linkId, share, title, expirationDate, password, denyDownload, primary, requiredAuth);
    }

    /// Sets an external link for the specified file or folder entry asynchronously.
    /// <param name="entry">The file or folder entry for which to set the external link.</param>
    /// <param name="linkId">The unique identifier of the existing link or a new GUID for creating a link.</param>
    /// <param name="share">The level of sharing permissions to assign to the link.</param>
    /// <param name="title">The title of the external link.</param>
    /// <param name="expirationDate">The expiration date of the link. Defaults to no expiration if not provided.</param>
    /// <param name="password">The optional password to secure the link. Defaults to null.</param>
    /// <param name="denyDownload">Indicates whether downloading should be denied through the link. Defaults to false.</param>
    /// <param name="primary">Indicates whether the link is the primary external link for the entry. Defaults to false.</param>
    /// <param name="requiredAuth">Indicates whether authentication is required to access the link. Defaults to false.</param>
    /// <typeparam name="T">The type parameter for the entry object.</typeparam>
    /// <returns>A task representing the asynchronous operation. Upon completion, returns an AceWrapper object containing the external share link details.</returns>
    public async Task<AceWrapper> SetExternalLinkAsync<T>(FileEntry<T> entry, Guid linkId, FileShare share, string title, DateTime expirationDate = default,
        string password = null, bool denyDownload = false, bool primary = false, bool requiredAuth = false)
    {
        var options = new FileShareOptions { Title = !string.IsNullOrEmpty(title) ? title : FilesCommonResource.DefaultExternalLinkTitle, DenyDownload = denyDownload, Internal = requiredAuth };

        var expirationDateUtc = tenantUtil.DateTimeToUtc(expirationDate);
        if (expirationDateUtc != DateTime.MinValue && expirationDateUtc > DateTime.UtcNow)
        {
            if (expirationDateUtc > DateTime.UtcNow.AddYears(FilesLinkUtility.MaxLinkLifeTimeInYears))
            {
                throw new ArgumentException(null, nameof(expirationDate));
            }

            options.ExpirationDate = expirationDateUtc;
        }

        if (!string.IsNullOrEmpty(password))
        {
            options.Password = password;
        }

        var actions = entry.FileEntryType == FileEntryType.File
            ? _fileMessageActions
            : _roomMessageActions;

        var result = await SetAceLinkAsync(entry, primary ? SubjectType.PrimaryExternalLink : SubjectType.ExternalLink, linkId, share, options);
        if (result == null)
        {
            return (await fileSharing.GetPureSharesAsync(entry, [linkId]).FirstOrDefaultAsync());
        }

        var (eventType, previousRecord, ace) = result;

        linkId = ace.Id;

        if (eventType == EventType.Remove && ace.SubjectType == SubjectType.PrimaryExternalLink &&
            (entry is Folder<T> { FolderType: FolderType.PublicRoom or FolderType.FillingFormsRoom } room))
        {
            linkId = Guid.NewGuid();

            var (defaultTitle, defaultAccess) = room.FolderType switch
            {
                FolderType.PublicRoom => (FilesCommonResource.DefaultExternalLinkTitle, FileShare.Read),
                FolderType.FillingFormsRoom => (FilesCommonResource.FillOutExternalLinkTitle, FileShare.FillForms),
                _ => throw new InvalidOperationException()
            };

            result = await SetAceLinkAsync(entry, SubjectType.PrimaryExternalLink, linkId, defaultAccess, new FileShareOptions { Title = defaultTitle });

            await filesMessageService.SendAsync(MessageAction.RoomExternalLinkRevoked, entry, linkId.ToString(), ace.FileShareOptions?.Title,
                result.Ace.FileShareOptions?.Title);

            return (await fileSharing.GetPureSharesAsync(entry, [linkId]).FirstOrDefaultAsync());
        }

        var isRoom = entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType);

        if (eventType is EventType.Update)
        {
            var previousTitle = previousRecord.Options?.Title != ace.FileShareOptions?.Title
                ? previousRecord.Options?.Title
                : null;

            if (!string.IsNullOrEmpty(previousTitle))
            {
                await filesMessageService.SendAsync(MessageAction.RoomExternalLinkRenamed, entry, ace.Id.ToString(), ace.FileShareOptions?.Title, previousRecord.Options?.Title);
            }
        }

        if (eventType != EventType.Remove)
        {
            await filesMessageService.SendAsync(actions[SubjectType.ExternalLink][eventType], entry, ace.FileShareOptions?.Title, FileShareExtensions.GetAccessString(ace.Access, isRoom), ace.Id.ToString());
        }
        else
        {
            await filesMessageService.SendAsync(actions[SubjectType.ExternalLink][eventType], entry, ace.FileShareOptions?.Title);
        }

        return (await fileSharing.GetPureSharesAsync(entry, [linkId]).FirstOrDefaultAsync());
    }

    /// Removes access control entries (ACEs) from the specified files and folders asynchronously.
    /// <param name="filesId">A list of file IDs for which to remove the ACEs.</param>
    /// <param name="foldersId">A list of folder IDs for which to remove the ACEs.</param>
    /// <typeparam name="T">The type of the identifiers for files and folders.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result does not return a value.</returns>
    public async Task RemoveAceAsync<T>(List<T> filesId, List<T> foldersId)
    {
        if (!authContext.IsAuthenticated)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        foreach (var fileId in filesId)
        {
            var entry = await fileDao.GetFileAsync(fileId);
            await fileSharingAceHelper.RemoveAceAsync(entry);
            await filesMessageService.SendAsync(MessageAction.FileRemovedFromList, entry, entry.Title);
        }

        foreach (var folderId in foldersId)
        {
            var entry = await folderDao.GetFolderAsync(folderId);
            await fileSharingAceHelper.RemoveAceAsync(entry);
            await filesMessageService.SendAsync(MessageAction.FolderRemovedFromList, entry, entry.Title);
        }
    }

    /// Checks asynchronously whether a given file or folder is publicly accessible.
    /// <param name="entryId">The identifier of the entry (file or folder) to check.</param>
    /// <typeparam name="T">The type of the entry identifier.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the entry is publicly accessible; otherwise, false.</returns>
    public async Task<bool> IsPublicAsync<T>(T entryId)
    {
        var entry = await GetEntryAsync(entryId, FileEntryType.Folder);
        return await fileSharing.IsPublicAsync(entry);
    }

    /// Retrieves the count of pure shares for a specified file or folder entry asynchronously.
    /// <param name="entryId">The identifier of the file or folder entry.</param>
    /// <param name="entryType">The type of the entry, either file or folder.</param>
    /// <param name="filterType">The filter type to refine the query, such as user, group, or link shares.</param>
    /// <param name="text">Optional search text to filter the shares by specific criteria.</param>
    /// <typeparam name="T">The type of the identifier for the file or folder entry.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result contains the count of pure shares matching the specified criteria.</returns>
    public async Task<int> GetPureSharesCountAsync<T>(T entryId, FileEntryType entryType, ShareFilterType filterType, string text)
    {
        var entry = await GetEntryAsync(entryId, entryType);

        return await fileSharing.GetPureSharesCountAsync(entry, filterType, text);
    }

    /// Sets an invitation link with the specified properties asynchronously.
    /// <param name="roomId">The identifier of the folder or room for which the invitation link is being set.</param>
    /// <param name="linkId">The unique identifier of the invitation link to be set.</param>
    /// <param name="title">The title for the invitation link. If not provided, a default title will be used.</param>
    /// <param name="share">The sharing permissions associated with the invitation link.</param>
    /// <typeparam name="T">The type of the folder or room identifier.</typeparam>
    /// <returns>A task that represents the asynchronous operation. The task result contains an AceWrapper object with the details of the created or updated invitation link.</returns>
    public async Task<AceWrapper> SetInvitationLinkAsync<T>(T roomId, Guid linkId, string title, FileShare share)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId)).NotFoundIfNull();

        var options = new FileShareOptions
        {
            Title = !string.IsNullOrEmpty(title)
                ? title
                : FilesCommonResource.DefaultInvitationLinkTitle,
            ExpirationDate = DateTime.UtcNow.Add(invitationValidator.IndividualLinkExpirationInterval)
        };

        var result = await SetAceLinkAsync(room, SubjectType.InvitationLink, linkId, share, options);

        await filesMessageService.SendAsync(_roomMessageActions[SubjectType.InvitationLink][result.EventType], room, result.Ace.Id, result.Ace.FileShareOptions?.Title,
            FileShareExtensions.GetAccessString(result.Ace.Access, true));

        return (await fileSharing.GetPureSharesAsync(room, [result.Ace.Id]).FirstOrDefaultAsync());
    }
    
    public async IAsyncEnumerable<FileEntry> ChangeOwnerAsync<T>(IEnumerable<T> foldersId, IEnumerable<T> filesId, Guid userId, FileShare newShare = FileShare.RoomManager)
    {
        var userInfo = await userManager.GetUsersAsync(userId);
        if (Equals(userInfo, Constants.LostUser) ||
            userInfo.Status != EmployeeStatus.Active ||
            await userManager.IsGuestAsync(userInfo) ||
            await userManager.IsUserAsync(userInfo))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ChangeOwner);
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var folders = folderDao.GetFoldersAsync(foldersId);

        await foreach (var folder in folders)
        {
            if (folder.RootFolderType is not FolderType.COMMON and not FolderType.VirtualRooms and not FolderType.RoomTemplates)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (!await fileSecurity.CanChangeOwnerAsync(folder))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);

            if (folder.ProviderEntry && !isRoom)
            {
                continue;
            }

            var newFolder = folder;
            if (folder.CreateBy != userInfo.Id)
            {
                var createBy = folder.CreateBy;

                await SetAceObjectAsync(new AceCollection<T>
                {
                    Files = [],
                    Folders = [folder.Id],
                    Aces =
                    [
                        new AceWrapper { Access = FileShare.None, Id = userInfo.Id },
                        new AceWrapper { Access = newShare, Id = createBy }
                    ]
                }, false, socket: false, beforeOwnerChange: true);

                var folderAccess = folder.Access;

                newFolder.CreateBy = userInfo.Id;

                if (folder.ProviderEntry && isRoom)
                {
                    var providerDao = daoFactory.ProviderDao;
                    await providerDao.UpdateRoomProviderInfoAsync(new ProviderData { Id = folder.ProviderId, CreateBy = userInfo.Id });
                }
                else
                {
                    var newFolderId = await folderDao.SaveFolderAsync(newFolder);
                    newFolder = await folderDao.GetFolderAsync(newFolderId);
                    newFolder.Access = folderAccess;

                    await entryStatusManager.SetIsFavoriteFolderAsync(folder);
                }

                await filesMessageService.SendAsync(MessageAction.FileChangeOwner, newFolder, [
                    newFolder.Title, userInfo.DisplayUserName(false, displayUserSettingsHelper)
                ]);

                await webhookManager.PublishAsync(WebhookTrigger.FolderUpdated, newFolder);
            }

            yield return newFolder;
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var files = fileDao.GetFilesAsync(filesId);

        await foreach (var file in files)
        {
            if (!await fileSecurity.CanChangeOwnerAsync(file))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (await lockerManager.FileLockedForMeAsync(file.Id))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_LockedFile);
            }

            if (await fileTracker.IsEditingAsync(file.Id))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UpdateEditingFile);
            }

            if (file.RootFolderType != FolderType.COMMON)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (file.ProviderEntry)
            {
                continue;
            }

            var newFile = file;
            if (file.CreateBy != userInfo.Id)
            {
                newFile = serviceProvider.GetService<File<T>>();
                newFile.Id = file.Id;
                newFile.Version = file.Version + 1;
                newFile.VersionGroup = file.VersionGroup + 1;
                newFile.Title = file.Title;
                newFile.SetFileStatus(await file.GetFileStatus());
                newFile.ParentId = file.ParentId;
                newFile.CreateBy = userInfo.Id;
                newFile.CreateOn = file.CreateOn;
                newFile.ConvertedType = file.ConvertedType;
                newFile.Comment = FilesCommonResource.CommentChangeOwner;
                newFile.Encrypted = file.Encrypted;
                newFile.ThumbnailStatus = file.ThumbnailStatus == Thumbnail.Created ? Thumbnail.Creating : Thumbnail.Waiting;

                await using (var stream = await fileDao.GetFileStreamAsync(file))
                {
                    newFile.ContentLength = stream.CanSeek ? stream.Length : file.ContentLength;
                    newFile = await fileDao.SaveFileAsync(newFile, stream);
                }

                if (file.ThumbnailStatus == Thumbnail.Created)
                {
                    foreach (var size in thumbnailSettings.Sizes)
                    {
                        await (await globalStore.GetStoreAsync()).CopyAsync(String.Empty,
                            fileDao.GetUniqThumbnailPath(file, size.Width, size.Height),
                            String.Empty,
                            fileDao.GetUniqThumbnailPath(newFile, size.Width, size.Height));
                    }

                    await fileDao.SetThumbnailStatusAsync(newFile, Thumbnail.Created);

                    newFile.ThumbnailStatus = Thumbnail.Created;
                }

                await fileMarker.MarkAsNewAsync(newFile);

                await entryStatusManager.SetFileStatusAsync(newFile);

                await filesMessageService.SendAsync(MessageAction.FileChangeOwner, newFile, [
                    newFile.Title, userInfo.DisplayUserName(false, displayUserSettingsHelper)
                ]);

                await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, newFile);
            }

            yield return newFile;
        }
    }
    
    private async Task<ProcessedItem<T>> SetAceLinkAsync<T>(FileEntry<T> entry, SubjectType subjectType, Guid linkId, FileShare share, FileShareOptions options)
    {
        if (linkId == Guid.Empty)
        {
            linkId = Guid.NewGuid();
        }

        var aces = new List<AceWrapper> { new() { Access = share, Id = linkId, SubjectType = subjectType, FileShareOptions = options } };

        try
        {
            var result = await fileSharingAceHelper.SetAceObjectAsync(aces, entry, false, null);
            if (!string.IsNullOrEmpty(result.Warning))
            {
                throw GenerateException(new InvalidOperationException(result.Warning));
            }

            var processedItem = result.ProcessedItems[0];

            return !result.Changed ? processedItem : result.ProcessedItems[0];
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }
    
    private async Task<FileEntry<T>> GetEntryAsync<T>(T entryId, FileEntryType entryType)
    {
        FileEntry<T> entry = entryType == FileEntryType.Folder
            ? await daoFactory.GetFolderDao<T>().GetFolderAsync(entryId)
            : await daoFactory.GetFileDao<T>().GetFileAsync(entryId);

        return entry.NotFoundIfNull();
    }
    
    private Exception GenerateException(Exception error, bool warning = false)
    {
        if (warning || error is ItemNotFoundException or SecurityException or ArgumentException or TenantQuotaException or InvalidOperationException)
        {
            logger.Information(error.ToString());
        }
        else
        {
            logger.ErrorFileStorageService(error);
        }

        if (error is ItemNotFoundException)
        {
            return !authContext.CurrentAccount.IsAuthenticated
                ? new SecurityException(FilesCommonResource.ErrorMessage_SecurityException)
                : error;
        }

        return new InvalidOperationException(error.Message, error);
    }
}