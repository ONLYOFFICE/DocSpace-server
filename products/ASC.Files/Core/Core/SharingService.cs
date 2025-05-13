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
    ILogger<FolderOperationsService> logger)
{
    private static readonly FrozenDictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> _roomMessageActions =
        new Dictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> { { SubjectType.InvitationLink, new Dictionary<EventType, MessageAction> { { EventType.Create, MessageAction.RoomInvitationLinkCreated }, { EventType.Update, MessageAction.RoomInvitationLinkUpdated }, { EventType.Remove, MessageAction.RoomInvitationLinkDeleted } }.ToFrozenDictionary() }, { SubjectType.ExternalLink, new Dictionary<EventType, MessageAction> { { EventType.Create, MessageAction.RoomExternalLinkCreated }, { EventType.Update, MessageAction.RoomExternalLinkUpdated }, { EventType.Remove, MessageAction.RoomExternalLinkDeleted } }.ToFrozenDictionary() } }.ToFrozenDictionary();

    private static readonly FrozenDictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> _fileMessageActions =
        new Dictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> { { SubjectType.ExternalLink, new Dictionary<EventType, MessageAction> { { EventType.Create, MessageAction.FileExternalLinkCreated }, { EventType.Update, MessageAction.FileExternalLinkUpdated }, { EventType.Remove, MessageAction.FileExternalLinkDeleted } }.ToFrozenDictionary() } }.ToFrozenDictionary();

    public async Task<List<AceWrapper>> GetSharedInfoAsync<T>(
        IEnumerable<T> fileIds,
        IEnumerable<T> folderIds)
    {
        return await fileSharing.GetSharedInfoAsync(fileIds, folderIds);
    }
    
    public async IAsyncEnumerable<AceWrapper> GetPureSharesAsync<T>(T entryId, FileEntryType entryType, ShareFilterType filterType, string text, int offset, int count)
    {
        var entry = await GetEntryAsync(entryId, entryType);

        await foreach (var ace in fileSharing.GetPureSharesAsync(entry, filterType, null, text, offset, count))
        {
            yield return ace;
        }
    }
    
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
    
    public async Task<AceWrapper> SetExternalLinkAsync<T>(T entryId, FileEntryType entryType, Guid linkId, string title, FileShare share, DateTime expirationDate = default,
        string password = null, bool denyDownload = false, bool requiredAuth = false, bool primary = false)
    {
        FileEntry<T> entry = entryType == FileEntryType.File
            ? await daoFactory.GetFileDao<T>().GetFileAsync(entryId)
            : await daoFactory.GetFolderDao<T>().GetFolderAsync(entryId);

        return await SetExternalLinkAsync(entry.NotFoundIfNull(), linkId, share, title, expirationDate, password, denyDownload, primary, requiredAuth);
    }

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
    
    public async Task<bool> IsPublicAsync<T>(T entryId)
    {
        var entry = await GetEntryAsync(entryId, FileEntryType.Folder);
        return await fileSharing.IsPublicAsync(entry);
    }

    public async Task<int> GetPureSharesCountAsync<T>(T entryId, FileEntryType entryType, ShareFilterType filterType, string text)
    {
        var entry = await GetEntryAsync(entryId, entryType);

        return await fileSharing.GetPureSharesCountAsync(entry, filterType, text);
    }
    
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