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

using Microsoft.AspNetCore.Http.Extensions;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Core;

[Singleton]
public class UserManagerConstants
{
    public IDictionary<Guid, UserInfo> SystemUsers { get; }
    internal Constants Constants { get; }

    public UserManagerConstants(Constants constants)
    {
        SystemUsers = Configuration.Constants.SystemAccounts.ToDictionary(a => a.ID, a => new UserInfo { Id = a.ID, LastName = a.Name });
        SystemUsers[Constants.LostUser.Id] = Constants.LostUser;
        SystemUsers[Constants.OutsideUser.Id] = Constants.OutsideUser;
        SystemUsers[constants.NamingPoster.Id] = constants.NamingPoster;
        Constants = constants;
    }
}

[Scope]
public class UserManager(
    IUserService userService,
    TenantManager tenantManager,
    PermissionContext permissionContext,
    UserManagerConstants userManagerConstants,
    CoreSettings coreSettings,
    InstanceCrypto instanceCrypto,
    RadicaleClient radicalClient,
    CardDavAddressbook cardDavAddressBook,
    ILogger<UserManager> log,
    ICache cache,
    TenantQuotaFeatureCheckerCount<CountPaidUserFeature> countPaidUserChecker,
    TenantQuotaFeatureCheckerCount<CountUserFeature> activeUsersFeatureChecker,
    Constants constants,
    IHttpContextAccessor httpContextAccessor,
    UserFormatter userFormatter,
    QuotaSocketManager quotaSocketManager,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
    IDistributedLockProvider distributedLockProvider)
{    
    private IDictionary<Guid, UserInfo> SystemUsers => userManagerConstants.SystemUsers;

    private Tenant Tenant => tenantManager.GetCurrentTenant();


    public void ClearCache()
    {
        if (userService is ICachedService service)
        {
            service.InvalidateCache();
        }
    }


    #region Users

    public async Task<UserInfo[]> GetUsersAsync()
    {
        return await GetUsersAsync(EmployeeStatus.Default);
    }

    public async Task<UserInfo[]> GetUsersAsync(EmployeeStatus status)
    {
        return await GetUsersAsync(status, EmployeeType.All);
    }

    public async Task<UserInfo[]> GetUsersAsync(EmployeeStatus status, EmployeeType type)
    {
        var users = (await GetUsersInternalAsync()).Where(u => (u.Status & status) == u.Status).ToAsyncEnumerable();
        switch (type)
        {
            case EmployeeType.RoomAdmin:
                users = users.WhereAwait(async u => !await this.IsUserAsync(u) && !await this.IsCollaboratorAsync(u) && !await this.IsDocSpaceAdminAsync(u));
                break;
            case EmployeeType.DocSpaceAdmin:
                users = users.WhereAwait(async u => await this.IsDocSpaceAdminAsync(u));
                break;
            case EmployeeType.Collaborator:
                users = users.WhereAwait(async u => await this.IsCollaboratorAsync(u));
                break;
            case EmployeeType.User:
                users = users.WhereAwait(async u => await this.IsUserAsync(u));
                break;
        }

        return await users.ToArrayAsync();
    }
    
    public async Task<UserInfo> GetUsersAsync(Guid id)
    {
        if (IsSystemUser(id))
        {
            return SystemUsers[id];
        }

        var u = await userService.GetUserAsync(Tenant.Id, id);

        return u is { Removed: false } ? u : Constants.LostUser;
    }
    
    public async Task<UserInfo> GetUserAsync(Guid id, Expression<Func<User, UserInfo>> exp)
    {
        if (IsSystemUser(id))
        {
            return SystemUsers[id];
        }

        var u = await userService.GetUserAsync(Tenant.Id, id, exp);

        return u is { Removed: false } ? u : Constants.LostUser;
    }
    
    public Task<int> GetUsersCountAsync(
        bool isDocSpaceAdmin,
        EmployeeStatus? employeeStatus,
        List<List<Guid>> includeGroups,
        List<Guid> excludeGroups,
        List<Tuple<List<List<Guid>>, List<Guid>>> combinedGroups,
        EmployeeActivationStatus? activationStatus,
        AccountLoginType? accountLoginType,
        string text,
        bool withoutGroup)
    {
        return userService.GetUsersCountAsync(Tenant.Id, isDocSpaceAdmin, employeeStatus, includeGroups, excludeGroups, combinedGroups, activationStatus, accountLoginType, text, withoutGroup);
    }

    public IAsyncEnumerable<UserInfo> GetUsers(
        bool isDocSpaceAdmin,
        EmployeeStatus? employeeStatus,
        List<List<Guid>> includeGroups,
        List<Guid> excludeGroups,
        List<Tuple<List<List<Guid>>, List<Guid>>> combinedGroups,
        EmployeeActivationStatus? activationStatus,
        AccountLoginType? accountLoginType,
        string text,
        bool withoutGroup,
        string sortBy,
        bool sortOrderAsc,
        long limit,
        long offset)
    {
        if (!UserSortTypeExtensions.TryParse(sortBy, true, out var sortType))
        {
            sortType = UserSortType.FirstName;
        }
        
        return userService.GetUsers(Tenant.Id, isDocSpaceAdmin, employeeStatus, includeGroups, excludeGroups, combinedGroups, activationStatus, accountLoginType, text, withoutGroup, Tenant.OwnerId, sortType, sortOrderAsc, limit, offset);
    }

    public UserInfo GetUsers(Guid id)
    {
        if (IsSystemUser(id))
        {
            return SystemUsers[id];
        }

        var u = userService.GetUser(Tenant.Id, id);

        return u is { Removed: false } ? u : Constants.LostUser;
    }
    
    public async Task<string[]> GetUserNamesAsync(EmployeeStatus status)
    {
        return (await GetUsersAsync(status))
            .Select(u => u.UserName)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }

    public async Task<UserInfo> GetUserByUserNameAsync(string username)
    {
        var u = await userService.GetUserByUserName(await tenantManager.GetCurrentTenantIdAsync(), username);

        return u ?? Constants.LostUser;
    }

    public async Task<UserInfo> GetUserBySidAsync(string sid)
    {
        return (await GetUsersInternalAsync())
                .FirstOrDefault(u => u.Sid != null && string.Equals(u.Sid, sid, StringComparison.CurrentCultureIgnoreCase)) ?? Constants.LostUser;
    }

    public async Task<UserInfo> GetSsoUserByNameIdAsync(string nameId)
    {
        return (await GetUsersInternalAsync())
            .FirstOrDefault(u => !string.IsNullOrEmpty(u.SsoNameId) && string.Equals(u.SsoNameId, nameId, StringComparison.CurrentCultureIgnoreCase)) ?? Constants.LostUser;
    }

    public async Task<UserInfo> GetUsersByPasswordHashAsync(int tenant, string login, string passwordHash)
    {
        var u = await userService.GetUserByPasswordHashAsync(tenant, login, passwordHash);

        return u is { Removed: false } ? u : Constants.LostUser;
    }

    public async Task<bool> UserExistsAsync(Guid id)
    {
        return UserExists(await GetUsersAsync(id));
    }

    public bool UserExists(Guid id)
    {
        return UserExists(GetUsers(id));
    }

    public bool UserExists(UserInfo user)
    {
        return !user.Equals(Constants.LostUser);
    }

    public bool IsSystemUser(Guid id)
    {
        return SystemUsers.ContainsKey(id);
    }

    public async Task<UserInfo> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Constants.LostUser;
        }

        var u = await userService.GetUserAsync(Tenant.Id, email);

        return u is { Removed: false } ? u : Constants.LostUser;
    }

    public async Task<UserInfo> SearchUserAsync(string id)
    {
        var result = Constants.LostUser;

        if (32 <= id.Length)
        {
            var guid = Guid.Empty;
            try
            {
                guid = new Guid(id);
            }
            catch (FormatException) { }
            catch (OverflowException) { }

            if (guid != Guid.Empty)
            {
                result = await GetUsersAsync(guid);
            }
        }

        if (Constants.LostUser.Equals(result))
        {
            result = await GetUserByEmailAsync(id);
        }

        if (Constants.LostUser.Equals(result))
        {
            result = await GetUserByUserNameAsync(id);
        }

        return result;
    }

    public async Task<UserInfo[]> SearchAsync(string text, EmployeeStatus status, Guid groupId)
    {
        if (text == null || text.Trim().Length == 0)
        {
            return Array.Empty<UserInfo>();
        }

        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return Array.Empty<UserInfo>();
        }

        var users = groupId == Guid.Empty ?
            await GetUsersAsync(status) :
            (await GetUsersByGroupAsync(groupId)).Where(u => (u.Status & status) == status);

        var findUsers = new List<UserInfo>();
        foreach (var user in users)
        {
            var properties = new[]
            {
                    user.LastName ?? string.Empty,
                    user.FirstName ?? string.Empty,
                    user.Title ?? string.Empty,
                    user.Location ?? string.Empty,
                    user.Email ?? string.Empty
            };
            if (IsPropertiesContainsWords(properties, words))
            {
                findUsers.Add(user);
            }
        }

        return findUsers.ToArray();
    }

    public async Task<UserInfo> UpdateUserInfoAsync(UserInfo u, bool afterInvite = false)
    {
        if (IsSystemUser(u.Id))
        {
            return SystemUsers[u.Id];
        }

        if (afterInvite)
        {
            await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(u.Id, await this.GetUserTypeAsync(u.Id)), Constants.Action_AddRemoveUser);
        }
        else
        {
            await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(u.Id), Constants.Action_EditUser);
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        if (u.Status == EmployeeStatus.Terminated && u.Id == tenant.OwnerId)
        {
            throw new InvalidOperationException("Can not disable tenant owner.");
        }

        var oldUserData = await userService.GetUserByUserName(tenant.Id, u.UserName);

        if (oldUserData == null || Equals(oldUserData, Constants.LostUser))
        {
            throw new InvalidOperationException("User not found.");
        }

        var (name, value) = ("", -1);

        if (!await IsUserInGroupAsync(oldUserData.Id, Constants.GroupUser.ID) &&
            oldUserData.Status != u.Status)
        {
            (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
            value = oldUserData.Status > u.Status ? ++value : --value;//crutch: data race
        }

        var newUserData = await userService.SaveUserAsync(tenant.Id, u);

        if (value > 0)
        {
            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
        }

        return newUserData;
    }

    public async Task<UserInfo> UpdateUserInfoWithSyncCardDavAsync(UserInfo u)
    {
        var newUser = await UpdateUserInfoAsync(u);

        return newUser;
    }

    public async Task<UserInfo> SaveUserInfo(UserInfo u, EmployeeType type = EmployeeType.RoomAdmin, bool syncCardDav = false, bool paidUserQuotaCheck = true)
    {
        if (IsSystemUser(u.Id))
        {
            return SystemUsers[u.Id];
        }

        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(u.Id, type), Constants.Action_AddRemoveUser);

        if (constants.MaxEveryoneCount <= (await GetUsersByGroupAsync(Constants.GroupEveryone.ID)).Length)
        {
            throw new TenantQuotaException("Maximum number of users exceeded");
        }

        var oldUserData = await userService.GetUserByUserName(await tenantManager.GetCurrentTenantIdAsync(), u.UserName);

        if (oldUserData != null && !Equals(oldUserData, Constants.LostUser))
        {
            throw new InvalidOperationException("User already exist.");
        }

        IDistributedLockHandle lockHandle = null;

        try
        {
            if (type is EmployeeType.User)
            {
                lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetUsersCountCheckKey(Tenant.Id));
                
                await activeUsersFeatureChecker.CheckAppend();
            }
            else if (paidUserQuotaCheck)
            {
                lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(Tenant.Id));
                
                await countPaidUserChecker.CheckAppend();
            }

            var newUser = await userService.SaveUserAsync(await tenantManager.GetCurrentTenantIdAsync(), u);
            if (syncCardDav)
            {
                await SyncCardDavAsync(u, oldUserData, newUser);
            }

            return newUser;
        }
        finally
        {
            if (lockHandle != null)
            {
                await lockHandle.ReleaseAsync();
            }
        }
    }

    private async Task SyncCardDavAsync(UserInfo u, UserInfo oldUserData, UserInfo newUser)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        var myUri = (httpContextAccessor?.HttpContext != null) ? httpContextAccessor.HttpContext.Request.GetDisplayUrl() :
                    (cache.Get<string>("REWRITE_URL" + tenant.Id) != null) ?
                    new Uri(cache.Get<string>("REWRITE_URL" + tenant.Id)).ToString() : tenant.GetTenantDomain(coreSettings);

        var rootAuthorization = cardDavAddressBook.GetSystemAuthorization();

        if (rootAuthorization != null)
        {
            var allUserEmails = (await GetDavUserEmailsAsync()).ToList();

            if (oldUserData != null && oldUserData.Status != newUser.Status && newUser.Status == EmployeeStatus.Terminated)
            {
                var userAuthorization = oldUserData.Email.ToLower() + ":" + instanceCrypto.Encrypt(oldUserData.Email);
                var requestUrlBook = cardDavAddressBook.GetRadicaleUrl(myUri, newUser.Email.ToLower(), true, true);
                var collection = await cardDavAddressBook.GetCollection(requestUrlBook, userAuthorization, myUri);
                if (collection.Completed && collection.StatusCode != 404)
                {
                    await cardDavAddressBook.Delete(myUri, newUser.Id, newUser.Email, tenant.Id);
                }
                foreach (var email in allUserEmails)
                {
                    var requestUrlItem = cardDavAddressBook.GetRadicaleUrl(myUri, email.ToLower(), true, true, itemID: newUser.Id.ToString());
                    try
                    {
                        var davItemRequest = new DavRequest
                        {
                            Url = requestUrlItem,
                            Authorization = rootAuthorization,
                            Header = myUri
                        };
                        await radicalClient.RemoveAsync(davItemRequest).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        log.ErrorWithException(ex);
                    }
                }
            }
            else
            {
                try
                {
                    var cardDavUser = new CardDavItem(u.Id, u.FirstName, u.LastName, u.UserName, u.BirthDate, u.Sex, u.Title, u.Email, u.ContactsList, u.MobilePhone);
                    try
                    {
                        await cardDavAddressBook.UpdateItemForAllAddBooks(allUserEmails, myUri, cardDavUser, await tenantManager.GetCurrentTenantIdAsync(), oldUserData != null && oldUserData.Email != newUser.Email ? oldUserData.Email : null);
                    }
                    catch (Exception ex)
                    {
                        log.ErrorWithException(ex);
                    }
                }
                catch (Exception ex)
                {
                    log.ErrorWithException(ex);
                }
            }
        }
    }

    public async Task<IEnumerable<string>> GetDavUserEmailsAsync()
    {
        return await userService.GetDavUserEmailsAsync(await tenantManager.GetCurrentTenantIdAsync());
    }

    public async Task<IEnumerable<int>> GetTenantsWithFeedsAsync(DateTime from)
    {
        return await userService.GetTenantsWithFeedsAsync(from);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        if (IsSystemUser(id))
        {
            return;
        }

        await permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);
        if (id == Tenant.OwnerId)
        {
            throw new InvalidOperationException("Can not remove tenant owner.");
        }

        var delUser = await GetUsersAsync(id);
        await userService.RemoveUserAsync(Tenant.Id, id);
        var tenant = await tenantManager.GetCurrentTenantAsync();

        try
        {
            var currentMail = delUser.Email.ToLower();
            var currentAccountPassword = instanceCrypto.Encrypt(currentMail);
            var userAuthorization = currentMail + ":" + currentAccountPassword;
            var rootAuthorization = cardDavAddressBook.GetSystemAuthorization();
            var myUri = (httpContextAccessor?.HttpContext != null) ? httpContextAccessor.HttpContext.Request.GetDisplayUrl() :
                (cache.Get<string>("REWRITE_URL" + tenant.Id) != null) ?
                new Uri(cache.Get<string>("REWRITE_URL" + tenant.Id)).ToString() : tenant.GetTenantDomain(coreSettings);
            var davUsersEmails = await GetDavUserEmailsAsync();
            var requestUrlBook = cardDavAddressBook.GetRadicaleUrl(myUri, delUser.Email.ToLower(), true, true);

            if (rootAuthorization != null)
            {
                var addBookCollection = await cardDavAddressBook.GetCollection(requestUrlBook, userAuthorization, myUri);
                if (addBookCollection.Completed && addBookCollection.StatusCode != 404)
                {
                    var davbookRequest = new DavRequest
                    {
                        Url = requestUrlBook,
                        Authorization = rootAuthorization,
                        Header = myUri
                    };
                    await radicalClient.RemoveAsync(davbookRequest).ConfigureAwait(false);
                }

                foreach (var email in davUsersEmails)
                {
                    var requestUrlItem = cardDavAddressBook.GetRadicaleUrl(myUri, email.ToLower(), true, true, itemID: delUser.Id.ToString());
                    try
                    {
                        var davItemRequest = new DavRequest
                        {
                            Url = requestUrlItem,
                            Authorization = rootAuthorization,
                            Header = myUri
                        };
                        await radicalClient.RemoveAsync(davItemRequest).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        log.ErrorWithException(ex);
                    }
                }
            }

        }
        catch (Exception ex)
        {
            log.ErrorWithException(ex);
        }
    }

    public async Task SaveUserPhotoAsync(Guid id, byte[] photo)
    {
        if (IsSystemUser(id))
        {
            return;
        }

        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(id), Constants.Action_EditUser);

        await userService.SetUserPhotoAsync(Tenant.Id, id, photo);
    }

    public async Task<byte[]> GetUserPhotoAsync(Guid id)
    {
        if (IsSystemUser(id))
        {
            return null;
        }

        return await userService.GetUserPhotoAsync(Tenant.Id, id);
    }

    public async Task<List<GroupInfo>> GetUserGroupsAsync(Guid id)
    {
        return await GetUserGroupsAsync(id, IncludeType.Distinct, Guid.Empty);
    }

    public async Task<List<GroupInfo>> GetUserGroupsAsync(Guid userID, IncludeType includeType)
    {
        return await GetUserGroupsAsync(userID, includeType, null);
    }

    private async Task<List<GroupInfo>> GetUserGroupsAsync(Guid userID, IncludeType includeType, Guid? categoryId)
    {
        var httpRequestDictionary = new HttpRequestDictionary<List<GroupInfo>>(httpContextAccessor?.HttpContext, "GroupInfo");
        var result = httpRequestDictionary.Get(userID.ToString());
        if (result is { Count: > 0 })
        {
            if (categoryId.HasValue)
            {
                result = result.Where(r => r.CategoryID.Equals(categoryId.Value)).ToList();
            }

            return result;
        }

        result = new List<GroupInfo>();
        var distinctUserGroups = new List<GroupInfo>();

        var refs = await GetRefsInternalAsync();
        IEnumerable<UserGroupRef> userRefs = null;
        if (refs is UserGroupRefStore store)
        {
            userRefs = store.GetRefsByUser(userID);
        }

        var userRefsContainsNotRemoved = userRefs?.Where(r => !r.Removed && r.RefType == UserGroupRefType.Contains).ToList();

        foreach (var g in (await GetGroupsInternalAsync()).Where(g => !categoryId.HasValue || g.CategoryID == categoryId))
        {
            if (((g.CategoryID == Constants.SysGroupCategoryId || userRefs == null) && IsUserInGroupInternal(userID, g.ID, refs)) ||
                (userRefsContainsNotRemoved != null && userRefsContainsNotRemoved.Any(r => r.GroupId == g.ID)))
            {
                distinctUserGroups.Add(g);
            }
        }

        if (IncludeType.Distinct == (includeType & IncludeType.Distinct))
        {
            result.AddRange(distinctUserGroups);
        }

        result.Sort((group1, group2) => string.Compare(group1.Name, group2.Name, StringComparison.Ordinal));

        httpRequestDictionary.Add(userID.ToString(), result);

        if (categoryId.HasValue)
        {
            result = result.Where(r => r.CategoryID.Equals(categoryId.Value)).ToList();
        }

        return result;
    }

    public async Task<bool> IsUserInGroupAsync(Guid userId, Guid groupId)
    {
        return IsUserInGroupInternal(userId, groupId, await GetRefsInternalAsync());
    }

    public bool IsUserInGroup(Guid userId, Guid groupId)
    {
        return IsUserInGroupInternal(userId, groupId, GetRefsInternal());
    }

    public async Task<UserInfo[]> GetUsersByGroupAsync(Guid groupId, EmployeeStatus employeeStatus = EmployeeStatus.Default)
    {
        var refs = await GetRefsInternalAsync();

        return (await GetUsersAsync(employeeStatus)).Where(u => IsUserInGroupInternal(u.Id, groupId, refs)).ToArray();
    }

    public async Task AddUserIntoGroupAsync(Guid userId, Guid groupId, bool dontClearAddressBook = false, bool notifyWebSocket = true)
    {
        if (Constants.LostUser.Id == userId || Constants.LostGroupInfo.ID == groupId)
        {
            return;
        }

        var user = await GetUsersAsync(userId);
        var isUser = await this.IsUserAsync(user);
        var isPaidUser = await IsPaidUserAsync(user);

        await permissionContext.DemandPermissionsAsync(new UserGroupObject(new UserAccount(user, await tenantManager.GetCurrentTenantIdAsync(), userFormatter), groupId),
            Constants.Action_EditGroups);

        await userService.SaveUserGroupRefAsync(Tenant.Id, new UserGroupRef(userId, groupId, UserGroupRefType.Contains));

        ResetGroupCache(userId);

        if (groupId == Constants.GroupUser.ID)
        {
            var tenant = await tenantManager.GetCurrentTenantAsync();
            var myUri = (httpContextAccessor?.HttpContext != null) ? httpContextAccessor.HttpContext.Request.GetDisplayUrl() :
                       (cache.Get<string>("REWRITE_URL" + tenant.Id) != null) ?
                       new Uri(cache.Get<string>("REWRITE_URL" + tenant.Id)).ToString() : tenant.GetTenantDomain(coreSettings);

            if (!dontClearAddressBook)
            {
                await cardDavAddressBook.Delete(myUri, user.Id, user.Email, tenant.Id);
            }
        }

        if (!notifyWebSocket)
        {
            return;
        }

        if (isUser && groupId != Constants.GroupUser.ID ||
            !isUser && !isPaidUser && groupId != Constants.GroupUser.ID)
        {
            var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
        }
    }

    public async Task RemoveUserFromGroupAsync(Guid userId, Guid groupId)
    {
        if (Constants.LostUser.Id == userId || Constants.LostGroupInfo.ID == groupId)
        {
            return;
        }

        var user = await GetUsersAsync(userId);
        var isUserBefore = await this.IsUserAsync(user);
        var isPaidUserBefore = await IsPaidUserAsync(user);

        await permissionContext.DemandPermissionsAsync(new UserGroupObject(new UserAccount(user, await tenantManager.GetCurrentTenantIdAsync(), userFormatter), groupId),
            Constants.Action_EditGroups);

        await userService.RemoveUserGroupRefAsync(Tenant.Id, userId, groupId, UserGroupRefType.Contains);

        ResetGroupCache(userId);

        var isUserAfter = await this.IsUserAsync(user);
        var isPaidUserAfter = await IsPaidUserAsync(user);

        if (isPaidUserBefore && !isPaidUserAfter && isUserAfter ||
            isUserBefore && !isUserAfter)
        {
            var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
        }
    }

    internal void ResetGroupCache(Guid userID)
    {
        new HttpRequestDictionary<List<GroupInfo>>(httpContextAccessor?.HttpContext, "GroupInfo").Reset(userID.ToString());
        new HttpRequestDictionary<List<Guid>>(httpContextAccessor?.HttpContext, "GroupInfoID").Reset(userID.ToString());
    }

    #endregion Users


    #region Company

    public async Task<GroupInfo[]> GetDepartmentsAsync()
    {
        return await GetGroupsAsync();
    }

    public async Task<Guid> GetDepartmentManagerAsync(Guid deparmentID)
    {
        var groupRef = await userService.GetUserGroupRefAsync(Tenant.Id, deparmentID, UserGroupRefType.Manager);

        if (groupRef == null)
        {
            return Guid.Empty;
        }

        return groupRef.UserId;
    }

    public Guid GetDepartmentManager(Guid deparmentID)
    {
        var groupRef = userService.GetUserGroupRef(Tenant.Id, deparmentID, UserGroupRefType.Manager);

        if (groupRef == null)
        {
            return Guid.Empty;
        }

        return groupRef.UserId;
    }

    public async Task SetDepartmentManagerAsync(Guid deparmentID, Guid userID)
    {
        var managerId = await GetDepartmentManagerAsync(deparmentID);
        if (managerId != Guid.Empty)
        {
            await userService.RemoveUserGroupRefAsync(
                Tenant.Id,
                managerId, deparmentID, UserGroupRefType.Manager);
        }
        if (userID != Guid.Empty)
        {
            await userService.SaveUserGroupRefAsync(
                Tenant.Id,
                new UserGroupRef(userID, deparmentID, UserGroupRefType.Manager));
        }
    }

    public async Task<UserInfo> GetCompanyCEOAsync()
    {
        var id = await GetDepartmentManagerAsync(Guid.Empty);

        return id != Guid.Empty ? await GetUsersAsync(id) : null;
    }

    public async Task SetCompanyCEOAsync(Guid userId)
    {
        await SetDepartmentManagerAsync(Guid.Empty, userId);
    }

    #endregion Company


    #region Groups

    public IAsyncEnumerable<GroupInfo> GetGroupsAsync(string text, Guid userId, bool manager, GroupSortType sortBy, bool sortOrderAsc, int offset = 0, int count = -1)
    {
        return userService.GetGroupsAsync(Tenant.Id, text, userId, manager, sortBy, sortOrderAsc, offset, count)
            .Select(group => new GroupInfo(group.CategoryId)
            {
                ID = group.Id,
                Name = group.Name,
                Sid = group.Sid
            });
    }

    public Task<int> GetGroupsCountAsync(string text, Guid userId, bool manager)
    {
        return userService.GetGroupsCountAsync(Tenant.Id, text, userId, manager);
    }
    
    public async Task<GroupInfo[]> GetGroupsAsync()
    {
        return await GetGroupsAsync(Guid.Empty);
    }

    public async Task<GroupInfo[]> GetGroupsAsync(Guid categoryID)
    {
        return (await GetGroupsInternalAsync())
            .Where(g => g.CategoryID == categoryID)
            .ToArray();
    }

    public async Task<GroupInfo> GetGroupInfoAsync(Guid groupID)
    {
        var group = await userService.GetGroupAsync(Tenant.Id, groupID) ?? 
                    ToGroup(Constants.SystemGroups.FirstOrDefault(r => r.ID == groupID) ?? Constants.LostGroupInfo);

        if (group == null)
        {
            return Constants.LostGroupInfo;
        }

        return new GroupInfo
        {
            ID = group.Id,
            CategoryID = group.CategoryId,
            Name = group.Name,
            Sid = group.Sid
        };
    }

    public async Task<GroupInfo> GetGroupInfoBySidAsync(string sid)
    {
        return (await GetGroupsInternalAsync())
            .SingleOrDefault(g => g.Sid == sid) ?? Constants.LostGroupInfo;
    }

    public async Task<GroupInfo> SaveGroupInfoAsync(GroupInfo g)
    {
        if (Constants.LostGroupInfo.Equals(g))
        {
            return Constants.LostGroupInfo;
        }

        if (Constants.SystemGroups.Any(b => b.ID == g.ID))
        {
            return Constants.SystemGroups.Single(b => b.ID == g.ID);
        }

        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups);

        var newGroup = await userService.SaveGroupAsync(Tenant.Id, ToGroup(g));

        return new GroupInfo(newGroup.CategoryId) { ID = newGroup.Id, Name = newGroup.Name, Sid = newGroup.Sid };
    }

    public async Task DeleteGroupAsync(Guid id)
    {
        if (Constants.LostGroupInfo.ID.Equals(id))
        {
            return;
        }

        if (Constants.SystemGroups.Any(b => b.ID == id))
        {
            return;
        }

        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups);

        await userService.RemoveGroupAsync(Tenant.Id, id);
    }

    #endregion Groups


    private bool IsPropertiesContainsWords(IEnumerable<string> properties, IEnumerable<string> words)
    {
        foreach (var w in words)
        {
            var find = false;
            foreach (var p in properties)
            {
                find = (2 <= w.Length) && (0 <= p.IndexOf(w, StringComparison.CurrentCultureIgnoreCase));
                if (find)
                {
                    break;
                }
            }
            if (!find)
            {
                return false;
            }
        }

        return true;
    }


    private async Task<IEnumerable<UserInfo>> GetUsersInternalAsync()
    {
        return (await userService.GetUsersAsync(Tenant.Id))
            .Where(u => !u.Removed);
    }

    private async Task<IEnumerable<GroupInfo>> GetGroupsInternalAsync()
    {
        return (await userService.GetGroupsAsync(Tenant.Id))
            .Where(g => !g.Removed)
            .Select(g => new GroupInfo(g.CategoryId) { ID = g.Id, Name = g.Name, Sid = g.Sid })
            .Concat(Constants.SystemGroups)
            .ToList();
    }

    private async Task<IDictionary<string, UserGroupRef>> GetRefsInternalAsync()
    {
        return await userService.GetUserGroupRefsAsync(Tenant.Id);
    }

    private IDictionary<string, UserGroupRef> GetRefsInternal()
    {
        return userService.GetUserGroupRefs(Tenant.Id);
    }

    private bool IsUserInGroupInternal(Guid userId, Guid groupId, IDictionary<string, UserGroupRef> refs)
    {
        if (groupId == Constants.GroupEveryone.ID)
        {
            return true;
        }
        if (groupId == Constants.GroupAdmin.ID && (Tenant.OwnerId == userId || userId == Configuration.Constants.CoreSystem.ID || userId == constants.NamingPoster.Id))
        {
            return true;
        }
        if (groupId == Constants.GroupUser.ID && userId == Constants.OutsideUser.Id)
        {
            return true;
        }

        UserGroupRef r;
        if (groupId == Constants.GroupManager.ID || groupId == Constants.GroupUser.ID || groupId == Constants.GroupCollaborator.ID)
        {
            var isUser = refs.TryGetValue(UserGroupRef.CreateKey(Tenant.Id, userId, Constants.GroupUser.ID, UserGroupRefType.Contains), out r) && !r.Removed;
            if (groupId == Constants.GroupUser.ID)
            {
                return isUser;
            }

            var isCollaborator = refs.TryGetValue(UserGroupRef.CreateKey(Tenant.Id, userId, Constants.GroupCollaborator.ID, UserGroupRefType.Contains), out r) && !r.Removed;
            if (groupId == Constants.GroupCollaborator.ID)
            {
                return isCollaborator;
            }

            if (groupId == Constants.GroupManager.ID)
            {
                return !isUser && !isCollaborator;
            }
        }

        return refs.TryGetValue(UserGroupRef.CreateKey(Tenant.Id, userId, groupId, UserGroupRefType.Contains), out r) && !r.Removed;
    }

    private Group ToGroup(GroupInfo g)
    {
        if (g == null)
        {
            return null;
        }

        return new Group
        {
            Id = g.ID,
            Name = g.Name,
            ParentId = g.Parent?.ID ?? Guid.Empty,
            CategoryId = g.CategoryID,
            Sid = g.Sid
        };
    }

    private async Task<bool> IsPaidUserAsync(UserInfo userInfo)
    {
        return await this.IsCollaboratorAsync(userInfo) || await this.IsDocSpaceAdminAsync(userInfo);
    }
}
