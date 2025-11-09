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

namespace ASC.Web.Files.Utils;

[Scope]
public class FileSharingAceHelper(
    FileSecurity fileSecurity,
    FileUtility fileUtility,
    UserManager userManager,
    AuthContext authContext,
    DocumentServiceHelper documentServiceHelper,
    FileMarker fileMarker,
    NotifyClient notifyClient,
    GlobalFolderHelper globalFolderHelper,
    PathProvider pathProvider,
    FileSharingHelper fileSharingHelper,
    FileTrackerHelper fileTracker,
    InvitationService invitationService,
    StudioNotifyService studioNotifyService,
    UserManagerWrapper userManagerWrapper,
    IUrlShortener urlShortener,
    IDistributedLockProvider distributedLockProvider,
    SocketManager socketManager,
    UserSocketManager usersocketManager,
    IDaoFactory daoFactory,
    ExternalShare externalShare,
    SettingsManager settingsManager,
    PasswordSettingsManager passwordSettingsManager)
{
    private const int MaxInvitationLinks = 1;

    public async Task<AceProcessingResult<T>> SetAceObjectAsync<T>(
        List<AceWrapper> aceWrappers,
        FileEntry<T> entry,
        bool notify,
        string message,
        string culture = null,
        bool socket = true,
        bool beforeOwnerChange = false)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!aceWrappers.TrueForAll(r => r.Id == authContext.CurrentAccount.ID && r.Access == FileShare.None) &&
            !beforeOwnerChange &&
            !await fileSharingHelper.CanSetAccessAsync(entry))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var handledAces = new List<ProcessedItem<T>>(aceWrappers.Count);
        var folder = entry as Folder<T>;
        var file = entry as File<T>;
        var room = folder != null && DocSpaceHelper.IsRoom(folder.FolderType) ? folder : null;

        var roomUrl = room != null
            ? room.FolderType == FolderType.AiRoom
                ? pathProvider.GetAgentUrl(room.Id.ToString())
                : pathProvider.GetRoomsUrl(room.Id.ToString(), false)
            : null;
        var entryType = entry.FileEntryType;
        var recipients = new Dictionary<Guid, FileShare>();
        var usersWithoutRight = new List<Guid>();
        var changed = false;
        string warning = null;
        var shares = await fileSecurity.GetPureSharesAsync(entry, aceWrappers.Select(a => a.Id)).ToDictionaryAsync(r => r.Subject);

        foreach (var w in aceWrappers.OrderByDescending(ace => ace.SubjectGroup))
        {
            if (entry.CreateBy == w.Id && (!beforeOwnerChange || w.Access != FileShare.RoomManager && w.Access != FileShare.ContentCreator))
            {
                continue;
            }

            var emailInvite = !string.IsNullOrEmpty(w.Email);
            var currentUser = await userManager.GetUsersAsync(w.Id, false);
            if ((currentUser.Status == EmployeeStatus.Terminated || currentUser.Removed) && w.Access != FileShare.None)
            {
                continue;
            }

            var currentUserType = await userManager.GetUserTypeAsync(currentUser);
            var existedShare = shares.Get(w.Id);
            var eventType = existedShare != null ? w.Access == FileShare.None ? EventType.Remove : EventType.Update : EventType.Create;

            if (existedShare != null)
            {
                w.SubjectType = existedShare.SubjectType;
            }

            if (existedShare == null && w.SubjectType == default)
            {
                var group = await userManager.GetGroupInfoAsync(w.Id);

                if (group.Removed)
                {
                    continue;
                }

                if (group.ID != Constants.LostGroupInfo.ID)
                {
                    w.SubjectType = SubjectType.Group;
                }
            }

            if (file != null)
            {
                if ((w.Access is not (FileShare.Read or FileShare.Restrict or FileShare.ReadWrite or FileShare.None) && !fileUtility.CanWebView(entry.Title))
                    || (entry.RootFolderType != FolderType.USER && entry.RootFolderType != FolderType.VirtualRooms))
                {
                    continue;
                }

                var fileAccesses = await fileSecurity.GetAccesses(file, w.SubjectType);
                if (fileAccesses == null || !fileAccesses.TryGetValue(w.SubjectType, out var access) || !access.Contains(w.Access))
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_RoleNotAvailable);
                }
            }

            if (room != null)
            {
                if (folder.RootId is int root && root == await globalFolderHelper.FolderRoomTemplatesAsync)
                {
                    if (w.Access != FileShare.Read && w.Access != FileShare.None || w.SubjectType != SubjectType.User && w.SubjectType != SubjectType.Group)
                    {
                        throw new InvalidOperationException(FilesCommonResource.ErrorMessage_RoleNotAvailable);
                    }
                }
                else
                {
                    if (!FileSecurity.AvailableRoomAccesses.TryGetValue(folder.FolderType, out var subjectAccesses)
                        || !subjectAccesses.TryGetValue(w.SubjectType, out var accesses) || !accesses.Contains(w.Access))
                    {
                        throw new InvalidOperationException(FilesCommonResource.ErrorMessage_RoleNotAvailable);
                    }
                }

                if (w.FileShareOptions != null)
                {
                    if (w.SubjectType == SubjectType.PrimaryExternalLink && room is { FolderType: FolderType.PublicRoom or FolderType.FillingFormsRoom })
                    {
                        w.FileShareOptions.ExpirationDate = default;
                    }
                }
            }
            else if (folder != null)
            {
                var folderAccesses = await fileSecurity.GetAccesses(folder);
                if (folderAccesses == null || !folderAccesses.TryGetValue(w.SubjectType, out var access) || !access.Contains(w.Access))
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_RoleNotAvailable);
                }
            }

            if (existedShare != null)
            {
                if (existedShare.Options?.Internal != w.FileShareOptions?.Internal && !await fileSecurity.CanEditInternalAsync(entry))
                {
                    continue;
                }

                if (existedShare.Options?.ExpirationDate != w.FileShareOptions?.ExpirationDate)
                {
                    if (folder?.FolderType == FolderType.PublicRoom && w.SubjectType == SubjectType.PrimaryExternalLink)
                    {
                        continue;
                    }

                    if (w.SubjectType != SubjectType.InvitationLink && !await fileSecurity.CanEditExpirationAsync(entry))
                    {
                        continue;
                    }
                }
            }

            if (w.SubjectType is SubjectType.PrimaryExternalLink or SubjectType.ExternalLink)
            {
                var roomType = room?.FolderType;

                if (roomType == null)
                {
                    var entryRoom = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(folder != null ? folder.Id : entry.ParentId).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));
                    roomType = entryRoom?.FolderType;
                }

                w.FileShareOptions.Internal = roomType switch
                {
                    FolderType.VirtualDataRoom => true,
                    FolderType.PublicRoom => false,
                    _ => w.FileShareOptions.Internal
                };
            }

            if (!string.IsNullOrEmpty(w.FileShareOptions?.Password))
            {
                if (eventType != EventType.Remove)
                {
                    var settings = await settingsManager.LoadAsync<PasswordSettings>();
                    passwordSettingsManager.CheckPassword(w.FileShareOptions.Password, settings);
                }

                w.FileShareOptions.Password = await externalShare.CreatePasswordKeyAsync(w.FileShareOptions.Password);
            }

            if (room != null && !w.IsLink && (existedShare == null || (!existedShare.IsLink && existedShare.SubjectType != SubjectType.Group)))
            {
                if (room.RootId is int root && root != await globalFolderHelper.FolderRoomTemplatesAsync && (!FileSecurity.AvailableUserAccesses.TryGetValue(currentUserType, out var userAccesses) ||
                                                                                                             !userAccesses.Contains(w.Access)))
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_RoleNotAvailable);
                }

                if (emailInvite)
                {
                    var user = await userManager.GetUserByEmailAsync(w.Email);
                    if (!user.Equals(Constants.LostUser))
                    {
                        w.Id = user.Id;
                        await userManager.AddUserRelationAsync(authContext.CurrentAccount.ID, user.Id);

                        if (user.ActivationStatus != EmployeeActivationStatus.Pending)
                        {
                            emailInvite = false;
                        }
                    }
                    else
                    {
                        try
                        {
                            user = await userManagerWrapper.AddInvitedUserAsync(w.Email, EmployeeType.Guest, culture, false);
                            await usersocketManager.AddGuestAsync(user);
                            w.Id = user.Id;
                        }
                        catch (Exception e)
                        {
                            warning ??= e.Message;
                            continue;
                        }
                    }
                }
                else
                {
                    if (w.Access != FileShare.None)
                    {
                        var user = await userManager.GetUserByEmailAsync(w.Email);
                        if (await userManager.IsGuestAsync(user)
                            && !(await userManager.IsUserInGroupAsync(user.Id, currentUser.Id))
                            && !(await userManager.IsDocSpaceAdminAsync(user)))
                        {
                            await usersocketManager.AddGuestAsync(user, false);
                        }
                    }
                }
            }

            var subjects = await fileSecurity.GetUserSubjectsAsync(w.Id);

            if (entry.RootFolderType == FolderType.COMMON && subjects.Contains(Constants.GroupAdmin.ID))
            {
                continue;
            }

            var share = w.Access;

            IDistributedLockHandle handle = null;

            try
            {
                if (w.IsLink && eventType == EventType.Create)
                {
                    var additionalLinksSettings = await fileSecurity.GetLinksSettings(entry, SubjectType.ExternalLink);
                    var primaryLinksSettings = await fileSecurity.GetLinksSettings(entry, SubjectType.PrimaryExternalLink);

                    if (primaryLinksSettings == 0)
                    {
                        primaryLinksSettings = additionalLinksSettings;
                    }

                    var (filter, maxCount) = w.SubjectType switch
                    {
                        SubjectType.InvitationLink => (ShareFilterType.InvitationLink, MaxInvitationLinks),
                        SubjectType.ExternalLink => (ShareFilterType.AdditionalExternalLink, additionalLinksSettings),
                        SubjectType.PrimaryExternalLink => (ShareFilterType.PrimaryExternalLink, primaryLinksSettings),
                        _ => (ShareFilterType.Link, -1)
                    };

                    if (maxCount >= 0)
                    {
                        handle = await distributedLockProvider.TryAcquireFairLockAsync($"{entry.Id}_{entry.FileEntryType}_links");

                        var linksCount = await fileSecurity.GetPureSharesCountAsync(entry, filter, null, null);
                        if (linksCount >= maxCount)
                        {
                            warning ??= string.Format(FilesCommonResource.ErrorMessage_MaxLinksCount, maxCount);
                            continue;
                        }
                    }
                }

                if (share == FileShare.None && w.SubjectType is SubjectType.PrimaryExternalLink or SubjectType.ExternalLink)
                {
                    var tagDao = daoFactory.GetTagDao<T>();
                    var tags = await tagDao.GetTagsAsync(entry.Id, entry.FileEntryType, TagType.RecentByLink, null, w.Id.ToString())
                        .ToListAsync();

                    if (tags.Count > 0)
                    {
                        await socketManager.RemoveFromSharedAsync(entry, tags.Select(t => t.Owner));
                    }
                }

                await fileSecurity.ShareAsync(entry.Id, entryType, w.Id, share, w.SubjectType, w.FileShareOptions, owner: existedShare?.Owner);
            }
            finally
            {
                if (handle != null)
                {
                    await handle.ReleaseAsync();
                }
            }

            if (socket && room != null && !w.IsLink)
            {
                if (share == FileShare.None && !await userManager.IsDocSpaceAdminAsync(w.Id))
                {
                    await socketManager.DeleteFolder(room, [w.Id]);
                }
                else if (existedShare == null)
                {
                    await socketManager.CreateFolderAsync(room, [w.Id]);
                }
                else
                {
                    await socketManager.ChangeAccessRightsAsync(room, w.Id, w.Access);
                }
            }

            changed = true;
            handledAces.Add(new ProcessedItem<T>(eventType, existedShare, w, file != null ? file : folder));

            if (emailInvite)
            {
                var link = invitationService.GetInvitationLink(w.Email, share, authContext.CurrentAccount.ID, entry.Id.ToString(), culture);
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);

                await studioNotifyService.SendEmailRoomInviteAsync(w.Email, entry.Title, shortenLink, culture, true);
            }
            else
            {
                if (notify && room != null && eventType == EventType.Create && !w.IsLink)
                {
                    var user = await userManager.GetUsersAsync(w.Id);

                    await studioNotifyService.SendEmailRoomInviteExistingUserAsync(user, room.Title, roomUrl);
                }
            }

            entry.Access = share;

            var listUsersId = new List<Guid>();

            if (w.SubjectGroup)
            {
                listUsersId = (await userManager.GetUsersByGroupAsync(w.Id)).Select(ui => ui.Id).ToList();
            }
            else
            {
                listUsersId.Add(w.Id);
            }

            listUsersId.Remove(authContext.CurrentAccount.ID);

            if (entryType == FileEntryType.File)
            {
                foreach (var uId in listUsersId)
                {
                    await fileTracker.ChangeRight(entry.Id, uId, true);
                }
            }

            var addRecipient = share == FileShare.Read
                               || share == FileShare.CustomFilter
                               || share == FileShare.ReadWrite
                               || share == FileShare.Review
                               || share == FileShare.FillForms
                               || share == FileShare.Comment
                               || share == FileShare.RoomManager
                               || share == FileShare.Editing
                               || share == FileShare.ContentCreator
                               || (share == FileShare.None && entry.RootFolderType == FolderType.COMMON);

            var removeNew = share == FileShare.Restrict || (share == FileShare.None
                                                            && entry.RootFolderType is FolderType.USER or FolderType.VirtualRooms or FolderType.Archive);

            listUsersId.ForEach(id =>
            {
                recipients.Remove(id);
                if (addRecipient)
                {
                    recipients.Add(id, share);
                }
                else if (removeNew)
                {
                    usersWithoutRight.Add(id);
                }
            });


            if (usersWithoutRight.Count > 0 && share == FileShare.None && w.SubjectType is SubjectType.User or SubjectType.Group)
            {
                var tagDao = daoFactory.GetTagDao<T>();
                var tags = await tagDao.GetTagsAsync(entry.Id, entry.FileEntryType, TagType.RecentByLink).ToListAsync();
                usersWithoutRight = usersWithoutRight.Except(tags.Select(r => r.Owner)).ToList();
            }
        }

        if (entryType == FileEntryType.File)
        {
            await documentServiceHelper.CheckUsersForDropAsync((File<T>)entry);
        }

        if (recipients.Count > 0 && entry.RootFolderType is FolderType.USER or FolderType.Privacy)
        {
            var recipientIds = recipients.Keys.ToList();

            if (file != null || folder != null || entry.ProviderEntry)
            {
                await fileMarker.MarkAsNewAsync(entry, recipientIds);
            }

            await socketManager.AddToSharedAsync(entry, users: recipientIds);

            if (notify)
            {
                await notifyClient.SendShareNoticeAsync(entry, recipients, message, culture);
            }
        }

        foreach (var userId in usersWithoutRight)
        {
            await fileMarker.RemoveMarkAsNewAsync(entry, userId);
        }

        if (usersWithoutRight.Count > 0 && entry.RootFolderType is FolderType.USER or FolderType.Privacy)
        {
            await socketManager.RemoveFromSharedAsync(entry, users: usersWithoutRight);
        }

        return new AceProcessingResult<T>(changed, warning, handledAces);
    }

    public async Task RemoveAceAsync<T>(FileEntry<T> entry)
    {
        if ((entry.RootFolderType != FolderType.USER && entry.RootFolderType != FolderType.Privacy && entry.RootFolderType != FolderType.VirtualRooms)
            || Equals(entry.RootId, await globalFolderHelper.FolderMyAsync)
            || Equals(entry.RootId, await globalFolderHelper.FolderPrivacyAsync))
        {
            return;
        }

        var currentId = authContext.CurrentAccount.ID;
        var entryType = entry.FileEntryType;
        var tagDao = daoFactory.GetTagDao<T>();

        List<Tag> tags = [Tag.Favorite(currentId, entry), Tag.Recent(currentId, entry)];
        tags.AddRange(await tagDao.GetTagsAsync(entry.Id, entry.FileEntryType, TagType.RecentByLink).ToListAsync());

        var currentShare = await fileSecurity.GetSharesAsync(entry, [currentId]);
        if (currentShare != null && currentShare.Any() || entry is Folder<T>)
        {
            var defaultShare = entry.RootFolderType == FolderType.USER
                ? fileSecurity.DefaultMyShare
                : fileSecurity.DefaultPrivacyShare;

            if (entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType))
            {
                defaultShare = FileShare.None;
            }

            await fileSecurity.ShareAsync(entry.Id, entryType, currentId, defaultShare);
            await socketManager.SelfRestrictionAsync(entry, currentId, defaultShare);
        }

        if (entryType == FileEntryType.File)
        {
            await documentServiceHelper.CheckUsersForDropAsync((File<T>)entry);
        }

        await fileMarker.RemoveMarkAsNewAsync(entry);
        await tagDao.RemoveTagsAsync(tags);
        await socketManager.RemoveFromFavoritesAsync(entry, [currentId]);
        await socketManager.RemoveFromRecentAsync(entry, [currentId]);

        if (entry.RootFolderType is FolderType.USER or FolderType.Privacy or FolderType.VirtualRooms)
        {
            await socketManager.RemoveFromSharedAsync(entry, users: [currentId]);
        }
    }
}

[Scope]
public class FileSharingHelper(
    Global global,
    GlobalFolderHelper globalFolderHelper,
    FileSecurity fileSecurity,
    AuthContext authContext,
    UserManager userManager)
{
    public async Task<bool> CanSetAccessAsync<T>(FileEntry<T> entry)
    {
        if (entry == null)
        {
            return false;
        }

        if (entry.RootFolderType == FolderType.COMMON && await global.IsDocSpaceAdministratorAsync)
        {
            return true;
        }

        if ((entry.RootFolderType is FolderType.Archive)
            && (entry is not IFolder folder || !DocSpaceHelper.IsRoom(folder.FolderType)))
        {
            return false;
        }

        if (await fileSecurity.CanEditAccessAsync(entry))
        {
            return true;
        }

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            return false;
        }

        return entry.RootFolderType == FolderType.Privacy
               && entry is File<T>
               && (Equals(entry.RootId, await globalFolderHelper.FolderPrivacyAsync) || await fileSecurity.CanShareAsync(entry));
    }
}

[Scope]
public class FileSharing(
    Global global,
    FileSecurity fileSecurity,
    AuthContext authContext,
    UserManager userManager,
    ILogger<FileSharing> logger,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IDaoFactory daoFactory,
    FileSharingHelper fileSharingHelper,
    InvitationService invitationService,
    ExternalShare externalShare,
    IUrlShortener urlShortener)
{
    public async Task<bool> CanSetAccessAsync<T>(FileEntry<T> entry)
    {
        return await fileSharingHelper.CanSetAccessAsync(entry);
    }

    public async Task<bool> IsPublicAsync<T>(FileEntry<T> entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!await fileSecurity.CanReadAsync(entry))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        return await fileSecurity.IsPublicAsync(entry);
    }

    public async IAsyncEnumerable<AceWrapper> GetPureSharesAsync<T>(FileEntry<T> entry, IEnumerable<Guid> subjects)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!await fileSecurity.CanReadAsync(entry))
        {
            logger.InfoUserCanTGetSharedInfo(authContext.CurrentAccount.ID, entry.FileEntryType, entry.Id.ToString()!, FileSecurity.FilesSecurityActions.Read.ToString());

            yield break;
        }

        var canEditAccess = await fileSecurity.CanEditAccessAsync(entry);
        var canEditInternal = await fileSecurity.CanEditInternalAsync(entry);
        var canEditExpiration = await fileSecurity.CanEditExpirationAsync(entry);
        await foreach (var record in fileSecurity.GetPureSharesAsync(entry, subjects))
        {
            yield return await ToAceAsync(entry, record, canEditAccess, canEditInternal, canEditExpiration);
        }
    }

    public async IAsyncEnumerable<AceWrapper> GetPureSharesAsync<T>(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text, int offset, int count)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        _ = await fileSecurity.SetSecurity(new[] { entry }.ToAsyncEnumerable()).ToListAsync();

        var (canEditAccess, canEditInternal, canEditExpiration) = await Task.WhenAll(
            fileSecurity.CanEditAccessAsync(entry),
            fileSecurity.CanEditInternalAsync(entry),
            fileSecurity.CanEditExpirationAsync(entry)
        ).ContinueWith(t => (t.Result[0], t.Result[1], t.Result[2]));

        var canAccess = entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType)
            ? await CheckAccessAsync(entry, filterType)
            : canEditAccess;

        if (!canAccess)
        {
            logger.InfoUserCanTGetSharedInfo(authContext.CurrentAccount.ID, entry.FileEntryType, entry.Id.ToString()!, filterType.ToString());

            yield break;
        }

        var allDefaultAces = await GetDefaultAcesAsync(entry, filterType, status, text).ToListAsync();
        var defaultAces = allDefaultAces.Skip(offset).Take(count).ToList();

        offset = Math.Max(defaultAces.Count > 0 ? 0 : offset - allDefaultAces.Count, 0);
        count -= defaultAces.Count;

        var records = fileSecurity.GetPureSharesAsync(entry, filterType, status, text, offset, count);

        foreach (var record in defaultAces)
        {
            yield return record;
        }

        await foreach (var record in records)
        {
            yield return await ToAceAsync(entry, record, canEditAccess, canEditInternal, canEditExpiration);
        }
    }

    public async Task<int> GetPureSharesCountAsync<T>(FileEntry<T> entry, ShareFilterType filterType, string text)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!await CheckAccessAsync(entry, filterType))
        {
            logger.InfoUserCanTGetSharedInfo(authContext.CurrentAccount.ID, entry.FileEntryType, entry.Id.ToString()!, filterType.ToString());

            return 0;
        }

        var defaultAces = await GetDefaultAcesAsync(entry, filterType, null, text).CountAsync();
        var sharesCount = await fileSecurity.GetPureSharesCountAsync(entry, filterType, null, text);

        return defaultAces + sharesCount;
    }

    public async Task<List<AceWrapper>> GetSharedInfoAsync<T>(FileEntry<T> entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!await fileSecurity.CanReadAsync(entry))
        {
            logger.InfoUserCanTGetSharedInfo(authContext.CurrentAccount.ID, entry.FileEntryType, entry.Id.ToString()!, FileSecurity.FilesSecurityActions.Read.ToString());

            return [];
        }

        var result = new List<AceWrapper>();
        var shares = await fileSecurity.GetSharesAsync(entry);
        var canEditAccess = await fileSecurity.CanEditAccessAsync(entry);
        var canEditInternal = await fileSecurity.CanEditInternalAsync(entry);
        var canEditExpiration = await fileSecurity.CanEditExpirationAsync(entry);
        var canReadLinks = await fileSecurity.CanReadLinksAsync(entry);

        var records = shares
            .GroupBy(r => r.Subject)
            .Select(g => g.OrderBy(r => r.Level)
                .ThenBy(r => r.Level)
                .ThenByDescending(r => r.Share, new FileShareRecord<T>.ShareComparer(entry.RootFolderType)).FirstOrDefault());

        foreach (var r in records)
        {
            if (r == null)
            {
                continue;
            }

            if (r.IsLink && !canReadLinks)
            {
                continue;
            }

            var ace = await ToAceAsync(entry, r, canEditAccess, canEditInternal, canEditExpiration);

            if (ace.SubjectType == SubjectType.Group && ace.Id == Constants.LostGroupInfo.ID)
            {
                await fileSecurity.RemoveSubjectAsync(r.Subject, true);
                continue;
            }


            result.Add(ace);
        }

        if (!result.Exists(w => w.Owner))
        {
            var ownerId = entry.RootFolderType == FolderType.USER ? entry.RootCreateBy : entry.CreateBy;
            var w = new AceWrapper
            {
                Id = ownerId,
                SubjectName = await global.GetUserNameAsync(ownerId),
                SubjectGroup = false,
                Access = FileShare.ReadWrite,
                Owner = true,
                CanEditAccess = false
            };

            result.Add(w);
        }

        if (result.Exists(w => w.Id == authContext.CurrentAccount.ID))
        {
            result.Single(w => w.Id == authContext.CurrentAccount.ID).LockedRights = true;
        }

        if (entry.RootFolderType == FolderType.COMMON)
        {
            if (result.TrueForAll(w => w.Id != Constants.GroupAdmin.ID))
            {
                var w = new AceWrapper
                {
                    Id = Constants.GroupAdmin.ID,
                    SubjectName = FilesCommonResource.Admin,
                    SubjectGroup = true,
                    Access = FileShare.ReadWrite,
                    Owner = false,
                    LockedRights = true
                };

                result.Add(w);
            }

            var index = result.FindIndex(w => w.Id == Constants.GroupEveryone.ID);
            if (index == -1)
            {
                var w = new AceWrapper
                {
                    Id = Constants.GroupEveryone.ID,
                    SubjectName = FilesCommonResource.Everyone,
                    SubjectGroup = true,
                    Access = fileSecurity.DefaultCommonShare,
                    Owner = false,
                    DisableRemove = true
                };

                result.Add(w);
            }
            else
            {
                result[index].DisableRemove = true;
            }
        }

        return result;
    }

    public async Task<List<AceWrapper>> GetSharedInfoAsync<T>(IEnumerable<T> fileIds, IEnumerable<T> folderIds)
    {
        if (!authContext.IsAuthenticated)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var result = new List<AceWrapper>();

        var fileDao = daoFactory.GetFileDao<T>();
        var files = await fileDao.GetFilesAsync(fileIds).ToListAsync();

        var folderDao = daoFactory.GetFolderDao<T>();
        var folders = await folderDao.GetFoldersAsync(folderIds).ToListAsync();

        var entries = files.Concat(folders.Select(FileEntry<T> (r) => r));

        foreach (var entry in entries)
        {
            IEnumerable<AceWrapper> acesForObject;
            try
            {
                acesForObject = await GetSharedInfoAsync(entry);
            }
            catch (Exception e)
            {
                logger.ErrorGetSharedInfo(e);

                throw new InvalidOperationException(e.Message, e);
            }

            foreach (var aceForObject in acesForObject)
            {
                var duplicate = result.Find(ace => ace.Id == aceForObject.Id);
                if (duplicate == null)
                {
                    if (result.Count > 0)
                    {
                        aceForObject.Owner = false;
                        aceForObject.Access = FileShare.Varies;
                    }

                    continue;
                }

                if (duplicate.Access != aceForObject.Access)
                {
                    aceForObject.Access = FileShare.Varies;
                }

                if (duplicate.Owner != aceForObject.Owner)
                {
                    aceForObject.Owner = false;
                    aceForObject.Access = FileShare.Varies;
                }

                result.Remove(duplicate);
            }

            var withoutAce = result.Where(ace =>
                acesForObject.FirstOrDefault(aceForObject =>
                    aceForObject.Id == ace.Id) == null);
            foreach (var ace in withoutAce)
            {
                ace.Access = FileShare.Varies;
            }

            var notOwner = result.Where(ace =>
                ace.Owner &&
                acesForObject.FirstOrDefault(aceForObject =>
                    aceForObject.Owner
                    && aceForObject.Id == ace.Id) == null);
            foreach (var ace in notOwner)
            {
                ace.Owner = false;
                ace.Access = FileShare.Varies;
            }

            result.AddRange(acesForObject);
        }


        var ownerAce = result.Find(ace => ace.Owner);
        result.Remove(ownerAce);

        var meAce = result.Find(ace => ace.Id == authContext.CurrentAccount.ID);
        result.Remove(meAce);

        result.Sort((x, y) => String.CompareOrdinal(x.SubjectName, y.SubjectName));

        if (ownerAce != null)
        {
            result = new List<AceWrapper> { ownerAce }.Concat(result).ToList();
        }

        if (meAce != null)
        {
            result = new List<AceWrapper> { meAce }.Concat(result).ToList();
        }

        return [.. result];
    }

    public async Task<List<AceShortWrapper>> GetSharedInfoShortFileAsync<T>(File<T> file)
    {
        var aces = await GetSharedInfoAsync([file.Id], []);
        var inRoom = file.RootFolderType is FolderType.VirtualRooms or FolderType.Archive;

        return
        [
            ..aces
                .Where(aceWrapper => aceWrapper.Access != FileShare.Restrict && aceWrapper.SubjectType != SubjectType.InvitationLink)
                .Select(aceWrapper => new AceShortWrapper(aceWrapper.SubjectName, FileShareExtensions.GetAccessString(aceWrapper.Access, inRoom), aceWrapper.IsLink))
        ];
    }

    public async IAsyncEnumerable<GroupMemberSecurity> GetGroupMembersAsync<T>(FileEntry<T> entry, Guid groupId, string text, int offset, int count)
    {
        if (entry == null || !await fileSecurity.CanReadAsync(entry))
        {
            yield break;
        }

        var securityDao = daoFactory.GetSecurityDao<T>();
        var canEditAccess = await fileSecurity.CanEditAccessAsync(entry);
        var userId = authContext.CurrentAccount.ID;

        await foreach (var member in securityDao.GetGroupMembersWithSecurityAsync(entry, groupId, text, offset, count))
        {
            var isOwner = entry.CreateBy == member.UserId;

            yield return new GroupMemberSecurity
            {
                User = await userManager.GetUsersAsync(member.UserId),
                GroupShare = member.GroupShare,
                UserShare = isOwner ? FileShare.RoomManager : member.UserShare,
                CanEditAccess = canEditAccess && !isOwner && userId != member.UserId,
                Owner = isOwner
            };
        }
    }

    public async Task<int> GetGroupMembersCountAsync<T>(FileEntry<T> entry, Guid groupId, string text)
    {
        if (entry == null || !await fileSecurity.CanReadAsync(entry))
        {
            return 0;
        }

        var securityDao = daoFactory.GetSecurityDao<T>();

        return await securityDao.GetGroupMembersWithSecurityCountAsync(entry, groupId, text);
    }

    private async Task<bool> CheckAccessAsync<T>(FileEntry<T> entry, ShareFilterType filterType)
    {
        if (!await fileSecurity.CanReadAsync(entry))
        {
            return false;
        }

        switch (filterType)
        {
            case ShareFilterType.User or ShareFilterType.Group or ShareFilterType.UserOrGroup:
            case ShareFilterType.PrimaryExternalLink when entry.RootFolderType is FolderType.VirtualRooms:
                return true;
            default:
                return await fileSecurity.CanReadLinksAsync(entry);
        }
    }

    private async IAsyncEnumerable<AceWrapper> GetDefaultAcesAsync<T>(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text)
    {
        if (filterType is not (ShareFilterType.User or ShareFilterType.UserOrGroup))
        {
            yield break;
        }

        var cachedFolderDao = daoFactory.GetCacheFolderDao<T>();
        var parents = await cachedFolderDao.GetFirstParentTypeFromFileEntryAsync(entry);
        AceWrapper owner;

        if (parents is null)
        {
            owner = new AceWrapper
            {
                Id = entry.CreateBy,
                SubjectName = await global.GetUserNameAsync(entry.CreateBy),
                SubjectGroup = false,
                Access = FileShare.ReadWrite,
                Owner = true,
                CanEditAccess = false
            };
        }
        else
        {
            owner = new AceWrapper
            {
                Id = parents.CreateBy,
                SubjectName = await global.GetUserNameAsync(parents.CreateBy),
                SubjectGroup = false,
                Access = FileShare.ReadWrite,
                Owner = true,
                CanEditAccess = false
            };
        }

        if (await ValidateUserActivationAndSearch(owner.Id, filterType, status, text))
        {
            yield return owner;
        }
    }

    private async Task<bool> ValidateUserActivationAndSearch(Guid userId, ShareFilterType filterType, EmployeeActivationStatus? status, string text)
    {
        if (status.HasValue && filterType == ShareFilterType.User)
        {
            var user = await userManager.GetUsersAsync(userId);

            if (user.ActivationStatus != status.Value)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(text))
        {
            text = text.ToLower().Trim();

            var user = await userManager.GetUsersAsync(userId);

            if (!(user.FirstName.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
                  user.LastName.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
                  user.Email.Contains(text, StringComparison.CurrentCultureIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<AceWrapper> ToAceAsync<T>(FileEntry<T> entry, FileShareRecord<T> record, bool canEditAccess, bool canEditInternal, bool canEditExpiration)
    {
        var w = new AceWrapper
        {
            Id = record.Subject,
            SubjectGroup = false,
            Access = record.Share,
            FileShareOptions = record.Options,
            SubjectType = record.SubjectType,
            CanEditInternal = canEditInternal,
            CanEditExpirationDate = canEditExpiration
        };

        w.CanEditAccess = authContext.CurrentAccount.ID != w.Id && w.SubjectType is SubjectType.User or SubjectType.Group && canEditAccess;

        if (!record.IsLink)
        {
            if (w.SubjectType == SubjectType.Group)
            {
                var group = await userManager.GetGroupInfoAsync(record.Subject);
                w.SubjectGroup = true;

                w.SubjectName = group.ID == Constants.GroupEveryone.ID ? FilesCommonResource.Everyone :
                    group.ID == Constants.GroupAdmin.ID ? FilesCommonResource.Admin : group.Name;
            }
            else
            {
                var user = await userManager.GetUsersAsync(record.Subject);
                w.SubjectName = user.DisplayUserName(false, displayUserSettingsHelper);
            }

            w.Owner = entry.RootFolderType == FolderType.USER
                ? entry.RootCreateBy == record.Subject
                : entry.CreateBy == record.Subject;
            w.LockedRights = record.Subject == authContext.CurrentAccount.ID;

            return w;
        }

        string link;

        if (record.SubjectType == SubjectType.InvitationLink)
        {
            link = invitationService.GetInvitationLink(record.Subject, authContext.CurrentAccount.ID);
        }
        else
        {
            var linkData = await externalShare.GetLinkDataAsync(entry, record.Subject);
            link = linkData.Url;
            w.RequestToken = linkData.Token;
        }

        w.SubjectName = record.Options.Title;
        w.Link = await urlShortener.GetShortenLinkAsync(link);
        w.SubjectGroup = true;
        w.CanEditAccess = false;
        w.FileShareOptions.Password = await externalShare.GetPasswordAsync(w.FileShareOptions.Password);
        w.SubjectType = record.SubjectType;
        var room = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(entry is Folder<T> folder ? folder.Id : entry.ParentId).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));
        if (room is { FolderType: FolderType.VirtualDataRoom })
        {
            w.CanEditDenyDownload = !room.SettingsDenyDownload;
        }

        if (room is { FolderType: FolderType.PublicRoom } && record.SubjectType == SubjectType.PrimaryExternalLink)
        {
            w.CanEditExpirationDate = false;
            w.CanRevoke = true;
        }

        if (room is { FolderType: FolderType.FillingFormsRoom })
        {
            w.CanRevoke = true;
        }

        return w;
    }
}

public record AceProcessingResult<T>(bool Changed, string Warning, List<ProcessedItem<T>> ProcessedItems);

public record ProcessedItem<T>(EventType EventType, FileShareRecord<T> PastRecord, AceWrapper Ace, FileEntry<T> Entry);

public enum EventType
{
    Update,
    Create,
    Remove
}