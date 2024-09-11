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
    PathProvider pathProvider,
    FileSharingHelper fileSharingHelper,
    FileTrackerHelper fileTracker,
    InvitationService invitationService,
    StudioNotifyService studioNotifyService,
    UserManagerWrapper userManagerWrapper,
    IUrlShortener urlShortener,
    IDistributedLockProvider distributedLockProvider,
    SocketManager socketManager,
    FilesLinkUtility filesLinkUtility,
    CountPaidUserStatistic paidUserStatistic,
    TenantManager tenantManager)
{
    private const int MaxInvitationLinks = 1;
    private const int MaxAdditionalExternalLinks = 5;
    private const int MaxPrimaryExternalLinks = 1;

    private static readonly HashSet<FileShare> _availableNotification =
    [
        FileShare.ReadWrite, 
        FileShare.RoomAdmin, 
        FileShare.PowerUser, 
        FileShare.Editing, 
        FileShare.CustomFilter, 
        FileShare.FillForms, 
        FileShare.Review, 
        FileShare.Comment,
        FileShare.Read
    ];
    
    public async Task<AceProcessingResult<T>> SetAceObjectAsync<T>(
        List<AceWrapper> aceWrappers, 
        FileEntry<T> entry, 
        bool notify, 
        string message, 
        string culture = null, 
        bool socket = true, 
        bool beforeOwnerChange = false,
        bool quotaSensitive = false)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!aceWrappers.TrueForAll(r => r.Id == authContext.CurrentAccount.ID && r.Access == FileShare.None) && 
            !await fileSharingHelper.CanSetAccessAsync(entry))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var shares = await fileSecurity.GetPureSharesAsync(
                entry, 
                aceWrappers.Select(a => a.Id))
            .ToDictionaryAsync(r => r.Subject);
        
        var preprocessed = new List<ProcessedItem<T>>();
        
        foreach (var w in aceWrappers)
        {
            if (entry.CreateBy == w.Id && (!beforeOwnerChange || w.Access != FileShare.RoomAdmin))
            {
                continue;
            }
            
            var existedShare = shares.Get(w.Id);
            if (existedShare != null)
            {
                w.SubjectType = existedShare.SubjectType;
            }
            
            var eventType = existedShare != null 
                ? w.Access == FileShare.None 
                    ? EventType.Remove 
                    : EventType.Update 
                : EventType.Create;

            if (!string.IsNullOrEmpty(w.Email))
            {
                w.SubjectType = SubjectType.User;
                preprocessed.Add(new ProcessedItem<T>(eventType, existedShare, w));
                continue;
            }

            if ((existedShare == null && w.SubjectType == default) || w.SubjectType == SubjectType.User)
            {
                var currentUser = await userManager.GetUsersAsync(w.Id);
                if (currentUser.Status == EmployeeStatus.Terminated && w.Access != FileShare.None)
                {
                    continue;
                }

                if (!currentUser.Equals(Constants.LostUser))
                {
                    w.SubjectType = SubjectType.User;
                    preprocessed.Add(new ProcessedUserItem<T>(eventType, existedShare, w, currentUser));
                    continue;
                }
            }
            
            if ((existedShare == null && w.SubjectType == default) || w.SubjectType == SubjectType.Group)
            {
                var group = await userManager.GetGroupInfoAsync(w.Id);
                if (group.Removed)
                {
                    continue;
                }

                if (group.ID != Constants.LostGroupInfo.ID)
                {
                    w.SubjectType = SubjectType.Group;
                    preprocessed.Add(new ProcessedItem<T>(eventType, existedShare, w));
                    continue;
                }
            }
            
            preprocessed.Add(new ProcessedItem<T>(eventType, existedShare, w));
        }
        
        var recipients = new Dictionary<Guid, FileShare>();
        var usersWithoutRight = new List<Guid>();

        var (userItems, warning, quotaIncreaseBy) = await ProcessUserAcesAsync(entry,
            preprocessed.Where(x => x.Ace.SubjectType == SubjectType.User),
            culture,
            socket,
            notify,
            recipients,
            usersWithoutRight,
            quotaSensitive);

        var groupItems = await ProcessGroupAcesAsync(entry,
                preprocessed.Where(x => x.Ace.SubjectType == SubjectType.Group),
                recipients,
                usersWithoutRight)
            .ToListAsync();

        var linksItems = await ProcessLinkAcesAsync(entry,
                preprocessed.Where(x => x.Ace.IsLink))
            .ToListAsync();
        
        if (entry.FileEntryType == FileEntryType.File)
        {
            await documentServiceHelper.CheckUsersForDropAsync((File<T>)entry);
        }
        
        if (recipients.Count > 0)
        {
            if (entry.ProviderEntry || 
                entry.FileEntryType == FileEntryType.File || 
                ((Folder<T>)entry).FoldersCount + ((Folder<T>)entry).FilesCount > 0)
            {
                await fileMarker.MarkAsNewAsync(entry, recipients.Keys.ToList());
            }

            if (notify && entry.RootFolderType is FolderType.USER or FolderType.Privacy)
            {
                await notifyClient.SendShareNoticeAsync(entry, recipients, message, culture);
            }
        }
        
        foreach (var userId in usersWithoutRight)
        {
            await fileMarker.RemoveMarkAsNewAsync(entry, userId);
        }
        
        return new AceProcessingResult<T>(
            userItems.Count > 0 || groupItems.Count > 0 || linksItems.Count > 0,
            warning,
            userItems.Concat(groupItems).Concat(linksItems),
            quotaIncreaseBy);
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
    
    private async IAsyncEnumerable<ProcessedItem<T>> ProcessLinkAcesAsync<T>(FileEntry<T> entry, IEnumerable<ProcessedItem<T>> preprocessedAces)
    {
        switch (entry)
        {
            case Folder<T> { IsRoom: true } room:
                {
                    foreach (var item in preprocessedAces)
                    {
                        if (!FileSecurity.AvailableRoomAccesses.TryGetValue(room.FolderType, out var subjectAccesses)
                            || !subjectAccesses.TryGetValue(item.Ace.SubjectType, out var accesses) || !accesses.Contains(item.Ace.Access))
                        {
                            continue;
                        }

                        if (item.Ace.FileShareOptions != null)
                        {
                            if (item.Ace.SubjectType == SubjectType.PrimaryExternalLink)
                            {
                                item.Ace.FileShareOptions.ExpirationDate = default;
                            }

                            if (item.Ace.SubjectType is SubjectType.PrimaryExternalLink or SubjectType.ExternalLink)
                            {
                                item.Ace.FileShareOptions.Internal = false;
                            }
                        }

                        await ShareLinkAsync(item);
                        yield return item;
                    }

                    break;
                }
            case File<T>:
                {
                    foreach (var item in preprocessedAces)
                    {
                        if ((item.Ace.Access is not (FileShare.Read or FileShare.Restrict or FileShare.None) && !fileUtility.CanWebView(entry.Title))
                            || entry.RootFolderType != FolderType.USER)
                        {
                            continue;
                        }

                        if (!FileSecurity.AvailableFileAccesses.TryGetValue(entry.RootFolderType, out var subjectAccesses)
                            || !subjectAccesses.TryGetValue(item.Ace.SubjectType, out var accesses) || !accesses.Contains(item.Ace.Access))
                        {
                            continue;
                        }

                        if (item.Ace.FileShareOptions != null && item.Ace.SubjectType is SubjectType.PrimaryExternalLink or SubjectType.ExternalLink)
                        {
                            item.Ace.FileShareOptions.Password = null;
                            item.Ace.FileShareOptions.DenyDownload = false;
                        }

                        if (item.Ace.FileShareOptions != null && item.EventType == EventType.Create && item.Ace.FileShareOptions.ExpirationDate == DateTime.MinValue)
                        {
                            item.Ace.FileShareOptions.ExpirationDate = DateTime.UtcNow.Add(filesLinkUtility.DefaultLinkLifeTime);
                        }
                
                        await ShareLinkAsync(item);
                        yield return item;
                    }

                    break;
                }
        }

        async Task ShareLinkAsync(ProcessedItem<T> item)
        {
            IDistributedLockHandle handle = null;

            try
            {
                if (item.EventType == EventType.Create)
                {
                    var (filter, maxCount) = item.Ace.SubjectType switch
                    {
                        SubjectType.InvitationLink => (ShareFilterType.InvitationLink, MaxInvitationLinks),
                        SubjectType.ExternalLink => (ShareFilterType.AdditionalExternalLink, MaxAdditionalExternalLinks),
                        SubjectType.PrimaryExternalLink => (ShareFilterType.PrimaryExternalLink, MaxPrimaryExternalLinks),
                        _ => (ShareFilterType.Link, -1)
                    };

                    if (maxCount > 0)
                    {
                        handle = await distributedLockProvider.TryAcquireFairLockAsync($"{entry.Id}_{entry.FileEntryType}_links");

                        var linksCount = await fileSecurity.GetPureSharesCountAsync(entry, filter, null, null);
                        if (linksCount >= maxCount)
                        {
                            return;
                        }
                    }
                }
                    
                await fileSecurity.ShareAsync(entry.Id, entry.FileEntryType, item.Ace.Id, item.Ace.Access, item.Ace.SubjectType, item.Ace.FileShareOptions);
            }
            finally
            {
                if (handle != null)
                {
                    await handle.ReleaseAsync();
                }
            }
        }
    }

    private async IAsyncEnumerable<ProcessedItem<T>> ProcessGroupAcesAsync<T>(FileEntry<T> entry, IEnumerable<ProcessedItem<T>> preprocessedAces, Dictionary<Guid, FileShare> recipients, List<Guid> usersWithoutRight)
    {
        if (entry is not Folder<T> room)
        {
            yield break;
        }

        foreach (var preprocessedAce in preprocessedAces)
        {
            var (_, _, ace) = preprocessedAce;

            if (ace.SubjectType != SubjectType.Group)
            {
                continue;
            }

            if (!FileSecurity.AvailableRoomAccesses.TryGetValue(room.FolderType, out var subjectAccesses)
                || !subjectAccesses.TryGetValue(SubjectType.Group, out var accesses) || !accesses.Contains(ace.Access))
            {
                continue;
            }

            await fileSecurity.ShareAsync(room.Id, FileEntryType.Folder, ace.Id, ace.Access, SubjectType.Group);

            if (entry.FileEntryType == FileEntryType.File)
            {
                await fileTracker.ChangeRight(entry.Id, ace.Id, true);
            }

            var usersIds = (await userManager.GetUsersByGroupAsync(ace.Id))
                .Where(u => u.Id != authContext.CurrentAccount.ID)
                .Select(u => u.Id);
            
            foreach (var userId in usersIds)
            {
                recipients.Remove(userId);
                if (_availableNotification.Contains(ace.Access))
                {
                    recipients.Add(userId, ace.Access);
                }

                if (ace.Access == FileShare.Restrict ||
                    (ace.Access == FileShare.None &&
                     entry.RootFolderType is FolderType.USER or FolderType.VirtualRooms or FolderType.Archive))
                {
                    usersWithoutRight.Add(userId);
                }

                yield return preprocessedAce;
            }
        }
    }

    private async Task<(List<ProcessedItem<T>> Items, string Warning, int OverflowedQuotaValue)> ProcessUserAcesAsync<T>(
        FileEntry<T> entry,
        IEnumerable<ProcessedItem<T>> items,
        string culture,
        bool socket,
        bool notify,
        Dictionary<Guid, FileShare> recipients,
        List<Guid> usersWithoutRight,
        bool quotaSensitive = false)
    {
        if (entry is not Folder<T> room)
        {
            return ([], null, 0);
        }
        
        var result = new List<ProcessedItem<T>>();

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var pendingItems = new List<ProcessedItem<T>>();
        var roomUrl = pathProvider.GetRoomsUrl(room.Id.ToString());
        string warning = null;

        var quotaIncreaseBy = 0;

        foreach (var item in items)
        {
            var (eventType, _, ace) = item;
            
            if (ace.SubjectType != SubjectType.User)
            {
                continue;
            }
            
            if (!FileSecurity.AvailableRoomAccesses.TryGetValue(room.FolderType, out var subjectAccesses) || 
                !subjectAccesses.TryGetValue(SubjectType.User, out var accesses) || 
                !accesses.Contains(ace.Access))
            {
                continue;
            }

            var isPaidShare = FileSecurity.PaidShares.Contains(ace.Access);

            if (!string.IsNullOrEmpty(ace.Email))
            {
                if (quotaSensitive && isPaidShare)
                {
                    quotaIncreaseBy++;
                }
                
                pendingItems.Add(item);
                continue;
            }
            
            if (isPaidShare && !await userManager.IsPaidUserAsync(ace.Id))
            {
                if (quotaSensitive)
                {
                    quotaIncreaseBy++;
                }
                
                pendingItems.Add(item);
                continue;
            }

            await ShareAsync(ace, eventType);
            
            result.Add(item);
        }
        
        if (pendingItems.Count == 0)
        {
            return (result, null, 0);
        }

        if (quotaIncreaseBy > 0)
        {
            var currentCount = await paidUserStatistic.GetValueAsync();
            var quota = await tenantManager.GetTenantQuotaAsync(tenantId);
            var maxCount = quota.GetFeature<CountPaidUserFeature>().Value;
        
            var expectedQuotaValue = currentCount + quotaIncreaseBy;
            
            if (maxCount < expectedQuotaValue)
            {
                warning = string.Format(Resource.TariffsFeature_manager_exception, maxCount);
                return (result, warning, expectedQuotaValue);
            }
        }

        foreach (var item in pendingItems)
        {
            var (eventType, _, ace) = item;

            if (!string.IsNullOrEmpty(ace.Email))
            {
                var userType = FileSecurity.GetTypeByShare(ace.Access);
                if (await InviteAndShareAsync(ace, userType))
                {
                    result.Add(item);
                }
                
                continue;
            }

            if (item is not ProcessedUserItem<T> userItem)
            {
                continue;
            }
            
            try
            {
                await userManagerWrapper.UpdateUserTypeAsync(userItem.User, FileSecurity.GetTypeByShare(ace.Access));
            }
            catch (TenantQuotaException e)
            {
                warning = e.Message;
                ace.Access = FileSecurity.GetHighFreeRole(room.FolderType);
            }
            catch (Exception e)
            {
                warning = e.Message;
            }
            
            await ShareAsync(ace, eventType);

            result.Add(item);
        }

        return (result, warning, 0);
        
        async Task NotifyAsync(AceWrapper ace, EventType eventType, bool newUser)
        {
            recipients.Remove(ace.Id);
            
            if (_availableNotification.Contains(ace.Access))
            {
                recipients.Add(ace.Id, ace.Access);
            }
            else if (ace.Access == FileShare.Restrict || 
                     (ace.Access == FileShare.None && 
                      entry.RootFolderType is FolderType.USER or FolderType.VirtualRooms or FolderType.Archive))
            {
                usersWithoutRight.Add(ace.Id);
            }
            
            if (socket && !newUser)
            {
                if (ace.Access == FileShare.None && !await userManager.IsDocSpaceAdminAsync(ace.Id))
                {
                    await socketManager.DeleteFolder(room, [ace.Id]);
                }
                else if (eventType == EventType.Create)
                {
                    await socketManager.CreateFolderAsync(room, [ace.Id]);
                }
            }
            
            if (newUser)
            {
                var link = await invitationService.GetInvitationLinkAsync(ace.Email, ace.Access, authContext.CurrentAccount.ID, entry.Id.ToString(), culture);
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);

                await studioNotifyService.SendEmailRoomInviteAsync(ace.Email, entry.Title, shortenLink, culture, true);
                
                return;
            }
            
            if (notify && eventType == EventType.Create)
            {
                var user = await userManager.GetUsersAsync(ace.Id);
                await studioNotifyService.SendEmailRoomInviteExistingUserAsync(user, room.Title, roomUrl);
            }
        }
        
        async Task<bool> InviteAndShareAsync(AceWrapper ace, EmployeeType userType)
        {
            try
            {
                var invitedUser = await userManagerWrapper.AddInvitedUserAsync(ace.Email, userType, culture);
                ace.Id = invitedUser.Id;
            }
            catch (TenantQuotaException e)
            {
                warning = e.Message;
                
                ace.Access = FileSecurity.GetHighFreeRole(room.FolderType);
                if (ace.Access == FileShare.None)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                warning = e.Message;
                return false;
            }
            
            await fileSecurity.ShareAsync(room.Id, FileEntryType.Folder, ace.Id, ace.Access);
            await NotifyAsync(ace, EventType.Create, true);

            return true;
        }

        async Task ShareAsync(AceWrapper ace, EventType eventType)
        {
            await fileSecurity.ShareAsync(room.Id, FileEntryType.Folder, ace.Id, ace.Access);
            await NotifyAsync(ace, eventType, false);
            
            if (entry.FileEntryType == FileEntryType.File)
            {
                await fileTracker.ChangeRight(entry.Id, ace.Id, true);
            }
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

        if (entry is File<T> { IsForm: true })
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
    InvitationService invitationService,
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
                await fileSecurity.RemoveSubjectAsync(r.Subject, true);
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

        result.Sort((x, y) => String.CompareOrdinal(x.SubjectName, y.SubjectName));

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
        var userId = authContext.CurrentAccount.ID;

        await foreach (var member in securityDao.GetGroupMembersWithSecurityAsync(entry, groupId, text, offset, count))
        {
            var isOwner = entry.CreateBy == member.UserId;
            var isDocSpaceAdmin = await userManager.IsDocSpaceAdminAsync(member.UserId);
            
            yield return new GroupMemberSecurity
            {
                User = await userManager.GetUsersAsync(member.UserId),
                GroupShare = member.GroupShare,
                UserShare = isOwner || isDocSpaceAdmin ? FileShare.RoomAdmin : member.UserShare,
                CanEditAccess = canEditAccess && !isOwner && userId != member.UserId && !isDocSpaceAdmin,
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

    private async Task<AceWrapper> ToAceAsync<T>(FileEntry<T> entry, FileShareRecord<T> record, bool canEditAccess)
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

        return w;
    }
}

public enum EventType
{
    Update,
    Create,
    Remove
}

public record AceProcessingResult<T>(bool Changed, string Warning, IEnumerable<ProcessedItem<T>> ProcessedItems, int OverflowedQuotaValue);
public record ProcessedItem<T>(EventType EventType, FileShareRecord<T> Record, AceWrapper Ace);
public record ProcessedUserItem<T>(EventType EventType, FileShareRecord<T> Record, AceWrapper Ace, UserInfo User) 
    : ProcessedItem<T>(EventType, Record, Ace);