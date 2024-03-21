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
    FileSharingHelper fileSharingHelper,
    FileTrackerHelper fileTracker,
    InvitationLinkService invitationLinkService,
    StudioNotifyService studioNotifyService,
    UserManagerWrapper userManagerWrapper,
    CountPaidUserChecker countPaidUserChecker,
    IUrlShortener urlShortener,
    IDistributedLockProvider distributedLockProvider,
    TenantManager tenantManager,
    SocketManager socketManager,
    IConfiguration configuration)
{
    private const int MaxInvitationLinks = 1;
    private const int MaxAdditionalExternalLinks = 5;
    private const int MaxPrimaryExternalLinks = 1;

    private TimeSpan _defaultLinkLifeTime;

    private TimeSpan DefaultLinkLifeTime
    {
        get
        {
            if (_defaultLinkLifeTime != default)
            {
                return _defaultLinkLifeTime;
            }

            if (!TimeSpan.TryParse(configuration["externalLink:defaultLifetime"], out var defaultLifetime))
            {
                defaultLifetime = TimeSpan.FromDays(7);
            }

            return _defaultLinkLifeTime = defaultLifetime;
        }
    }

    public async Task<AceProcessingResult> SetAceObjectAsync<T>(List<AceWrapper> aceWrappers, FileEntry<T> entry, bool notify, string message,
        AceAdvancedSettingsWrapper advancedSettings, string culture = null, bool socket = true)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!aceWrappers.TrueForAll(r => r.Id == authContext.CurrentAccount.ID && r.Access == FileShare.None) &&
            !await fileSharingHelper.CanSetAccessAsync(entry) && advancedSettings is not { InvitationLink: true })
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var handledAces = new List<Tuple<EventType, AceWrapper>>(aceWrappers.Count);
        var ownerId = entry.RootFolderType == FolderType.USER ? entry.RootCreateBy : entry.CreateBy;
        var room = entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType) ? folder : null;
        var entryType = entry.FileEntryType;
        var recipients = new Dictionary<Guid, FileShare>();
        var usersWithoutRight = new List<Guid>();
        var changed = false;
        string warning = null;
        var shares = await fileSecurity.GetPureSharesAsync(entry, aceWrappers.Select(a => a.Id)).ToDictionaryAsync(r => r.Subject);
        var currentUserId = authContext.CurrentAccount.ID;

        foreach (var w in aceWrappers.OrderByDescending(ace => ace.SubjectGroup))
        {
            if (entry.CreateBy == currentUserId && w.Id == currentUserId && w.Access != FileShare.RoomAdmin)
            {
                continue;
            }

            var emailInvite = !string.IsNullOrEmpty(w.Email);
            var currentUserType = await userManager.GetUserTypeAsync(w.Id);
            var userType = EmployeeType.User;
            var existedShare = shares.Get(w.Id);
            var eventType = existedShare != null ? w.Access == FileShare.None ? EventType.Remove : EventType.Update : EventType.Create;

            if (existedShare != null)
            {
                w.SubjectType = existedShare.SubjectType;
            }

            if (existedShare == null && w.SubjectType == default)
            {
                var group = await userManager.GetGroupInfoAsync(w.Id);

                if (group.ID != Constants.LostGroupInfo.ID)
                {
                    w.SubjectType = SubjectType.Group;
                }
            }

            if (entryType == FileEntryType.File)
            {
                if ((w.Access is not (FileShare.Read or FileShare.Restrict or FileShare.None) && !fileUtility.CanWebView(entry.Title))
                    || entry.RootFolderType != FolderType.USER)
                {
                    continue;
                }

                if (!FileSecurity.AvailableFileAccesses.TryGetValue(entry.RootFolderType, out var subjectAccesses)
                    || !subjectAccesses.TryGetValue(w.SubjectType, out var accesses) || !accesses.Contains(w.Access))
                {
                    continue;
                }

                if (w.FileShareOptions != null && w.SubjectType is SubjectType.PrimaryExternalLink or SubjectType.ExternalLink)
                {
                    w.FileShareOptions.Password = null;
                    w.FileShareOptions.DenyDownload = false;
                }

                if (eventType == EventType.Create && w.FileShareOptions.ExpirationDate == DateTime.MinValue)
                {
                    w.FileShareOptions.ExpirationDate = DateTime.UtcNow.Add(DefaultLinkLifeTime);
                }
            }

            if (room != null)
            {
                if (!FileSecurity.AvailableRoomAccesses.TryGetValue(room.FolderType, out var subjectAccesses)
                    || !subjectAccesses.TryGetValue(w.SubjectType, out var accesses) || !accesses.Contains(w.Access))
                {
                    continue;
                }

                if (w.FileShareOptions != null)
                {
                    if (w.SubjectType == SubjectType.PrimaryExternalLink)
                    {
                        w.FileShareOptions.ExpirationDate = default;
                    }

                    if (w.SubjectType is SubjectType.PrimaryExternalLink or SubjectType.ExternalLink)
                    {
                        w.FileShareOptions.Internal = false;
                    }
                }
            }

            if (room != null && !w.IsLink && (existedShare == null || (!existedShare.IsLink && existedShare.SubjectType != SubjectType.Group)))
            {
                var correctAccess = FileSecurity.AvailableUserAccesses.TryGetValue(currentUserType, out var userAccesses)
                                    && userAccesses.Contains(w.Access);

                if (currentUserType == EmployeeType.DocSpaceAdmin && !correctAccess)
                {
                    continue;
                }

                if (existedShare != null && !correctAccess)
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_RoleNotAvailable);
                }

                IDistributedLockHandle quotaLockHandle = null;
                var tenantId = await tenantManager.GetCurrentTenantIdAsync();

                try
                {
                    if (!correctAccess && currentUserType == EmployeeType.User)
                    {
                        quotaLockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenantId));
                        await countPaidUserChecker.CheckAppend();
                    }

                    userType = FileSecurity.GetTypeByShare(w.Access);

                    if (!emailInvite && currentUserType != EmployeeType.DocSpaceAdmin)
                    {
                        var user = await userManager.GetUsersAsync(w.Id);
                        await userManagerWrapper.UpdateUserTypeAsync(user, userType);
                    }
                }
                catch (TenantQuotaException e)
                {
                    warning ??= e.Message;
                    w.Access = FileSecurity.GetHighFreeRole(room.FolderType);

                    if (w.Access == FileShare.None)
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    warning ??= e.Message;
                    continue;
                }
                finally
                {
                    if (quotaLockHandle != null)
                    {
                        await quotaLockHandle.ReleaseAsync();
                    }
                }

                if (emailInvite)
                {
                    try
                    {
                        var user = await userManagerWrapper.AddInvitedUserAsync(w.Email, userType, culture);
                        w.Id = user.Id;
                    }
                    catch (Exception e)
                    {
                        warning ??= e.Message;
                        continue;
                    }
                }
            }

            var subjects = await fileSecurity.GetUserSubjectsAsync(w.Id);

            if (entry.RootFolderType == FolderType.COMMON && subjects.Contains(Constants.GroupAdmin.ID))
            {
                continue;
            }

            var share = w.Access;

            IDistributedLockHandle linkLockHandle = null;

            try
            {
                if (w.IsLink && eventType == EventType.Create)
                {
                    var (filter, maxCount) = w.SubjectType switch
                    {
                        SubjectType.InvitationLink => (ShareFilterType.InvitationLink, MaxInvitationLinks),
                        SubjectType.ExternalLink when room != null => (ShareFilterType.AdditionalExternalLink, MaxAdditionalExternalLinks),
                        SubjectType.PrimaryExternalLink => (ShareFilterType.PrimaryExternalLink, MaxPrimaryExternalLinks),
                        _ => (ShareFilterType.Link, -1)
                    };

                    if (maxCount > 0)
                    {
                        linkLockHandle = await distributedLockProvider.TryAcquireFairLockAsync($"{entry.Id}_{entry.FileEntryType}_links");

                        var linksCount = await fileSecurity.GetPureSharesCountAsync(entry, filter, null, null);
                        if (linksCount >= maxCount)
                        {
                            warning ??= string.Format(FilesCommonResource.ErrorMessage_MaxLinksCount, maxCount);
                            continue;
                        }
                    }
                }

                await fileSecurity.ShareAsync(entry.Id, entryType, w.Id, share, w.SubjectType, w.FileShareOptions);
            }
            finally
            {
                if (linkLockHandle != null)
                {
                    await linkLockHandle.ReleaseAsync();
                }
            }

            if (socket && room != null)
            {
                if (share == FileShare.None)
                {
                    await socketManager.DeleteFolder(room, new[] { w.Id });
                }
                else if (existedShare == null)
                {
                    await socketManager.CreateFolderAsync(room, new[] { w.Id });
                }
            }

            changed = true;
            handledAces.Add(new Tuple<EventType, AceWrapper>(eventType, w));

            if (emailInvite)
            {
                var link = await invitationLinkService.GetInvitationLinkAsync(w.Email, share, authContext.CurrentAccount.ID, entry.Id.ToString(), culture);
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);

                await studioNotifyService.SendEmailRoomInviteAsync(w.Email, entry.Title, shortenLink, culture);
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
                listUsersId.ForEach(uid => fileTracker.ChangeRight(entry.Id, uid, true));
            }

            var addRecipient = share == FileShare.Read
                               || share == FileShare.CustomFilter
                               || share == FileShare.ReadWrite
                               || share == FileShare.Review
                               || share == FileShare.FillForms
                               || share == FileShare.Comment
                               || share == FileShare.RoomAdmin
                               || share == FileShare.Editing
                               || share == FileShare.Collaborator
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
        }

        if (entryType == FileEntryType.File)
        {
            await documentServiceHelper.CheckUsersForDropAsync((File<T>)entry);
        }

        if (recipients.Count > 0)
        {
            if (entryType == FileEntryType.File
                || ((Folder<T>)entry).FoldersCount + ((Folder<T>)entry).FilesCount > 0
                || entry.ProviderEntry)
            {
                await fileMarker.MarkAsNewAsync(entry, recipients.Keys.ToList());
            }

            if (entry.RootFolderType is FolderType.USER or FolderType.Privacy
                && notify)
            {
                await notifyClient.SendShareNoticeAsync(entry, recipients, message, culture);
            }
        }

        if (advancedSettings != null && entryType == FileEntryType.File && ownerId == authContext.CurrentAccount.ID && fileUtility.CanWebView(entry.Title) && !entry.ProviderEntry)
        {
            await fileSecurity.ShareAsync(entry.Id, entryType, FileConstant.DenyDownloadId, advancedSettings.DenyDownload ? FileShare.Restrict : FileShare.None);
            await fileSecurity.ShareAsync(entry.Id, entryType, FileConstant.DenySharingId, advancedSettings.DenySharing ? FileShare.Restrict : FileShare.None);
        }

        foreach (var userId in usersWithoutRight)
        {
            await fileMarker.RemoveMarkAsNewAsync(entry, userId);
        }

        return new AceProcessingResult(changed, warning, handledAces);
    }

    public async Task RemoveAceAsync<T>(FileEntry<T> entry)
    {
        if ((entry.RootFolderType != FolderType.USER && entry.RootFolderType != FolderType.Privacy)
            || Equals(entry.RootId, await globalFolderHelper.FolderMyAsync)
            || Equals(entry.RootId, await globalFolderHelper.FolderPrivacyAsync))
        {
            return;
        }

        var entryType = entry.FileEntryType;
        await fileSecurity.ShareAsync(entry.Id, entryType, authContext.CurrentAccount.ID,
            entry.RootFolderType == FolderType.USER
                ? fileSecurity.DefaultMyShare
                : fileSecurity.DefaultPrivacyShare);

        if (entryType == FileEntryType.File)
        {
            await documentServiceHelper.CheckUsersForDropAsync((File<T>)entry);
        }

        await fileMarker.RemoveMarkAsNewAsync(entry);
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

        if ((entry.RootFolderType is FolderType.VirtualRooms or FolderType.Archive) 
            && (entry is not IFolder folder || !DocSpaceHelper.IsRoom(folder.FolderType)))
        {
            return false;
        }

        if (await fileSecurity.CanEditAccessAsync(entry))
        {
            return true;
        }

        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            return false;
        }

        if (entry.RootFolderType == FolderType.USER && Equals(entry.RootId, await globalFolderHelper.FolderMyAsync))
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
    InvitationLinkService invitationLinkService,
    ExternalShare externalShare,
    IUrlShortener urlShortener)
{
    public async Task<bool> CanSetAccessAsync<T>(FileEntry<T> entry)
    {
        return await fileSharingHelper.CanSetAccessAsync(entry);
    }

    public async IAsyncEnumerable<AceWrapper> GetPureSharesAsync<T>(FileEntry<T> entry, IEnumerable<Guid> subjects)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!await fileSecurity.CanReadAsync(entry))
        {
            logger.ErrorUserCanTGetSharedInfo(authContext.CurrentAccount.ID, entry.FileEntryType, entry.Id.ToString()!);

            yield break;
        }

        var canEditAccess = await fileSecurity.CanEditAccessAsync(entry);

        await foreach (var record in fileSecurity.GetPureSharesAsync(entry, subjects))
        {
            yield return await ToAceAsync(entry, record, canEditAccess);
        }
    }

    public async IAsyncEnumerable<AceWrapper> GetPureSharesAsync<T>(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text, int offset, int count)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        var canEditAccess = await fileSecurity.CanEditAccessAsync(entry);

        var canAccess = entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType)
            ? await CheckAccessAsync(entry, filterType)
            : canEditAccess;

        if (!canAccess)
        {
            logger.ErrorUserCanTGetSharedInfo(authContext.CurrentAccount.ID, entry.FileEntryType, entry.Id.ToString()!);

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
            yield return await ToAceAsync(entry, record, canEditAccess);
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
            logger.ErrorUserCanTGetSharedInfo(authContext.CurrentAccount.ID, entry.FileEntryType, entry.Id.ToString()!);

            return 0;
        }

        var defaultAces = await GetDefaultAcesAsync(entry, filterType, null, text).CountAsync();
        var sharesCount = await fileSecurity.GetPureSharesCountAsync(entry, filterType, null, text);

        return defaultAces + sharesCount;
    }

    public async Task<List<AceWrapper>> GetSharedInfoAsync<T>(FileEntry<T> entry, IEnumerable<SubjectType> subjectsTypes = null)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!await fileSecurity.CanReadAsync(entry))
        {
            logger.ErrorUserCanTGetSharedInfo(authContext.CurrentAccount.ID, entry.FileEntryType, entry.Id.ToString());

            return [];
        }

        var result = new List<AceWrapper>();
        var shares = await fileSecurity.GetSharesAsync(entry);
        var canEditAccess = await fileSecurity.CanEditAccessAsync(entry);
        var canReadLinks = await fileSecurity.CanReadLinksAsync(entry);

        var records = shares
            .GroupBy(r => r.Subject)
            .Select(g => g.OrderBy(r => r.Level)
                .ThenBy(r => r.Level)
                .ThenByDescending(r => r.Share, new FileShareRecord.ShareComparer(entry.RootFolderType)).FirstOrDefault());

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

            if (subjectsTypes != null && !subjectsTypes.Contains(r.SubjectType))
            {
                continue;
            }

            if (r.Subject == FileConstant.DenyDownloadId || r.Subject == FileConstant.DenySharingId)
            {
                continue;
            }

            var ace = await ToAceAsync(entry, r, canEditAccess);
            
            if (ace.SubjectType == SubjectType.Group && ace.Id == Constants.LostGroupInfo.ID)
            {
                await fileSecurity.RemoveSubjectAsync<T>(r.Subject, true);
                continue;
            }

            result.Add(ace);
        }

        if (!result.Exists(w => w.Owner) && (subjectsTypes == null || subjectsTypes.Contains(SubjectType.User) || subjectsTypes.Contains(SubjectType.Group)))
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

    public async Task<List<AceWrapper>> GetSharedInfoAsync<T>(IEnumerable<T> fileIds, IEnumerable<T> folderIds, IEnumerable<SubjectType> subjectTypes = null)
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

        var entries = files.Concat(folders.Cast<FileEntry<T>>());

        foreach (var entry in entries)
        {
            IEnumerable<AceWrapper> acesForObject;
            try
            {
                acesForObject = await GetSharedInfoAsync(entry, subjectTypes);
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

        result.Sort((x, y) => string.Compare(x.SubjectName, y.SubjectName));

        if (ownerAce != null)
        {
            result = new List<AceWrapper> { ownerAce }.Concat(result).ToList();
        }

        if (meAce != null)
        {
            result = new List<AceWrapper> { meAce }.Concat(result).ToList();
        }

        return [..result];
    }

    public async Task<List<AceShortWrapper>> GetSharedInfoShortFileAsync<T>(File<T> file)
    {
        var aces = await GetSharedInfoAsync(new List<T> { file.Id }, new List<T>());
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

        await foreach (var member in securityDao.GetGroupMembersWithSecurityAsync(entry, groupId, text, offset, count))
        {
            var isOwner = entry.CreateBy == member.UserId;
            
            yield return new GroupMemberSecurity
            {
                User = await userManager.GetUsersAsync(member.UserId),
                GroupShare = member.GroupShare,
                UserShare = member.UserShare,
                CanEditAccess = canEditAccess && !isOwner,
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

        if (filterType is ShareFilterType.User or ShareFilterType.Group or ShareFilterType.UserOrGroup)
        {
            return true;
        }

        return await fileSecurity.CanReadLinksAsync(entry);
    }

    private async IAsyncEnumerable<AceWrapper> GetDefaultAcesAsync<T>(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text)
    {
        if (filterType is not (ShareFilterType.User or ShareFilterType.UserOrGroup))
        {
            yield break;
        }

        if (status.HasValue && filterType == ShareFilterType.User)
        {
            var user = await userManager.GetUsersAsync(entry.CreateBy);

            if (user.ActivationStatus != status.Value)
            {
                yield break;
            }
        }

        if (!string.IsNullOrEmpty(text))
        {
            text = text.ToLower().Trim();

            var user = await userManager.GetUsersAsync(entry.CreateBy);

            if (!(user.FirstName.ToLower().Contains(text) || user.LastName.ToLower().Contains(text) || user.Email.ToLower().Contains(text)))
            {
                yield break;
            }
        }

        var owner = new AceWrapper
        {
            Id = entry.CreateBy,
            SubjectName = await global.GetUserNameAsync(entry.CreateBy),
            SubjectGroup = false,
            Access = FileShare.ReadWrite,
            Owner = true,
            CanEditAccess = false
        };

        yield return owner;
    }

    private async Task<AceWrapper> ToAceAsync<T>(FileEntry<T> entry, FileShareRecord record, bool canEditAccess)
    {
        var w = new AceWrapper
        {
            Id = record.Subject,
            SubjectGroup = false,
            Access = record.Share,
            FileShareOptions = record.Options,
            SubjectType = record.SubjectType
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
            link = invitationLinkService.GetInvitationLink(record.Subject, authContext.CurrentAccount.ID);
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

        return w;
    }
}

public class AceProcessingResult(bool changed, string warning, IReadOnlyList<Tuple<EventType, AceWrapper>> handledAces)
{
    public bool Changed { get; } = changed;
    public string Warning { get; } = warning;
    public IReadOnlyList<Tuple<EventType, AceWrapper>> HandledAces { get; } = handledAces;
}

public enum EventType
{
    Update,
    Create,
    Remove
}