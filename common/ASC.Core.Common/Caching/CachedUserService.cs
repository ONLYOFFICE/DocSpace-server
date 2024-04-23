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

namespace ASC.Core.Caching;

[Singleton]
public class UserServiceCache
{
    private const string Users = "users";
    private const string Refs = "refs";
    private const string Groups = "groups";

    internal readonly ICache Cache;
    internal readonly ICacheNotify<UserInfoCacheItem> CacheUserInfoItem;
    internal readonly ICacheNotify<UserPhotoCacheItem> CacheUserPhotoItem;
    internal readonly ICacheNotify<GroupCacheItem> CacheGroupCacheItem;
    internal readonly ICacheNotify<UserGroupRefCacheItem> CacheUserGroupRefItem;

    public UserServiceCache(
        ICacheNotify<UserInfoCacheItem> cacheUserInfoItem,
        ICacheNotify<UserPhotoCacheItem> cacheUserPhotoItem,
        ICacheNotify<GroupCacheItem> cacheGroupCacheItem,
        ICacheNotify<UserGroupRefCacheItem> cacheUserGroupRefItem,
        ICache cache)
    {
        Cache = cache;
        CacheUserInfoItem = cacheUserInfoItem;
        CacheUserPhotoItem = cacheUserPhotoItem;
        CacheGroupCacheItem = cacheGroupCacheItem;
        CacheUserGroupRefItem = cacheUserGroupRefItem;

        cacheUserInfoItem.Subscribe(InvalidateCache, CacheNotifyAction.Any);
        cacheUserPhotoItem.Subscribe(p => Cache.Remove(p.Key), CacheNotifyAction.Remove);
        cacheGroupCacheItem.Subscribe(InvalidateCache, CacheNotifyAction.Any);

        cacheUserGroupRefItem.Subscribe(r => UpdateUserGroupRefCache(r), CacheNotifyAction.Remove);
        cacheUserGroupRefItem.Subscribe(r => UpdateUserGroupRefCache(r), CacheNotifyAction.InsertOrUpdate);
    }

    private void InvalidateCache(UserInfoCacheItem userInfo)
    {
        if (userInfo != null)
        {
            var key = GetUserCacheKey(userInfo.Tenant);
            Cache.Remove(key);

            if (Guid.TryParse(userInfo.Id, out var userId))
            {
                var userKey = GetUserCacheKey(userInfo.Tenant, userId);
                Cache.Remove(userKey);
            }
        }
    }
    private void InvalidateCache(GroupCacheItem groupCacheItem)
    {
        if (groupCacheItem == null)
        {
            return;
        }

        Cache.Remove(GetGroupCacheKey(groupCacheItem.Tenant, new Guid(groupCacheItem.Id)));
        Cache.Remove(GetGroupCacheKey(groupCacheItem.Tenant));
    }

    private void UpdateUserGroupRefCache(UserGroupRef r)
    {
        var key = GetRefCacheKey(r.TenantId);
        var usersRefs = Cache.Get<UserGroupRefStore>(key);
        if (usersRefs != null)
        {
            lock (usersRefs)
            {
                usersRefs[r.CreateKey()] = r;
            }
        }

        var groupRef = GetRefCacheKey(r.TenantId, r.GroupId, r.RefType);

        if (groupRef != null && r.Removed)
        {
            Cache.Remove(groupRef);
    }
    }

    public static string GetUserPhotoCacheKey(int tenant, Guid userId)
    {
        return tenant + "userphoto" + userId;
    }

    public static string GetGroupCacheKey(int tenant)
    {
        return tenant + Groups;
    }

    public static string GetGroupCacheKey(int tenant, Guid groupId)
    {
        return tenant + Groups + groupId;
    }

    public static string GetRefCacheKey(int tenant)
    {
        return tenant + Refs;
    }
    public static string GetRefCacheKey(int tenant, Guid groupId, UserGroupRefType refType)
    {
        return tenant.ToString() + groupId + (int)refType;
    }

    public static string GetUserCacheKey(int tenant)
    {
        return tenant + Users;
    }

    public static string GetUserCacheKey(int tenant, Guid userId)
    {
        return tenant + Users + userId;
    }
}

[Scope]
public class CachedUserService : IUserService, ICachedService
{
    private readonly EFUserService _service;
    private readonly ICache _cache;
    private readonly ICacheNotify<UserInfoCacheItem> _cacheUserInfoItem;
    private readonly ICacheNotify<UserPhotoCacheItem> _cacheUserPhotoItem;
    private readonly ICacheNotify<GroupCacheItem> _cacheGroupCacheItem;
    private readonly ICacheNotify<UserGroupRefCacheItem> _cacheUserGroupRefItem;

    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _photoExpiration;

    public CachedUserService(
        EFUserService service,
        UserServiceCache userServiceCache,
        ICacheNotify<GroupCacheItem> cacheGroupCacheItem
        )
    {
        _service = service;
        _cacheExpiration = TimeSpan.FromMinutes(20);
        _photoExpiration = TimeSpan.FromMinutes(10);
        _cacheGroupCacheItem = cacheGroupCacheItem;
        _cache = userServiceCache.Cache;
        _cacheUserInfoItem = userServiceCache.CacheUserInfoItem;
        _cacheUserPhotoItem = userServiceCache.CacheUserPhotoItem;
        _cacheGroupCacheItem = userServiceCache.CacheGroupCacheItem;
        _cacheUserGroupRefItem = userServiceCache.CacheUserGroupRefItem;
    }

    public Task<int> GetUsersCountAsync(
        int tenant,
        bool isDocSpaceAdmin,
        EmployeeStatus? employeeStatus,
        List<List<Guid>> includeGroups,
        List<Guid> excludeGroups,
        List<Tuple<List<List<Guid>>, List<Guid>>> combinedGroups,
        EmployeeActivationStatus? activationStatus,
        AccountLoginType? accountLoginType,
        QuotaFilter? quotaFilter,
        string text,
        bool withoutGroup)
    {
        return _service.GetUsersCountAsync(tenant, isDocSpaceAdmin, employeeStatus, includeGroups, excludeGroups, combinedGroups, activationStatus, accountLoginType, quotaFilter, text, withoutGroup);
    }

    public IAsyncEnumerable<UserInfo> GetUsers(
        int tenant,
        bool isDocSpaceAdmin,
        EmployeeStatus? employeeStatus,
        List<List<Guid>> includeGroups,
        List<Guid> excludeGroups,
        List<Tuple<List<List<Guid>>, List<Guid>>> combinedGroups,
        EmployeeActivationStatus? activationStatus,
        AccountLoginType? accountLoginType,
        QuotaFilter? quotaFilter,
        string text,
        bool withoutGroup,
        Guid ownerId,
        UserSortType sortBy,
        bool sortOrderAsc,
        long limit,
        long offset)
    {
        return _service.GetUsers(tenant, isDocSpaceAdmin, employeeStatus, includeGroups, excludeGroups, combinedGroups, activationStatus, accountLoginType, quotaFilter, text, withoutGroup, ownerId, sortBy, 
            sortOrderAsc, limit, offset);
    }

    public async Task<UserInfo> GetUserAsync(int tenant, Guid id)
    {
        var key = UserServiceCache.GetUserCacheKey(tenant, id);
        var user = _cache.Get<UserInfo>(key);

        if (user == null)
        {
            user = await _service.GetUserAsync(tenant, id);

            if (user != null)
            {
                _cache.Insert(key, user, _cacheExpiration);
            }
        }

        return user;
    }

    public UserInfo GetUser(int tenant, Guid id)
    {
        var key = UserServiceCache.GetUserCacheKey(tenant, id);
        var user = _cache.Get<UserInfo>(key);

        if (user == null)
        {
            user = _service.GetUser(tenant, id);

            if (user != null)
            {
                _cache.Insert(key, user, _cacheExpiration);
            }
        }

        return user;
    }

    public async Task<UserInfo> GetUserAsync(int tenant, string email)
    {
        return await _service.GetUserAsync(tenant, email);
    }

    public async Task<UserInfo> GetUserByUserName(int tenant, string userName)
    {
        return await _service.GetUserByUserName(tenant, userName);
    }

    public async Task<UserInfo> GetUserByPasswordHashAsync(int tenant, string login, string passwordHash)
    {
        return await _service.GetUserByPasswordHashAsync(tenant, login, passwordHash);
    }
    public async Task<IEnumerable<UserInfo>> GetUsersAllTenantsAsync(IEnumerable<Guid> userIds)
    {
        return await _service.GetUsersAllTenantsAsync(userIds);
    }

    public async Task<UserInfo> SaveUserAsync(int tenant, UserInfo user)
    {
        user = await _service.SaveUserAsync(tenant, user);
        await _cacheUserInfoItem.PublishAsync(new UserInfoCacheItem { Id = user.Id.ToString(), Tenant = tenant }, CacheNotifyAction.Any);

        return user;
    }

    public async Task<IEnumerable<int>> GetTenantsWithFeedsAsync(DateTime from)
    {
        return await _service.GetTenantsWithFeedsAsync(from);
    }

    public async Task RemoveUserAsync(int tenant, Guid id, bool immediate = false)
    {
        await _service.RemoveUserAsync(tenant, id, immediate);
        await _cacheUserInfoItem.PublishAsync(new UserInfoCacheItem { Tenant = tenant, Id = id.ToString() }, CacheNotifyAction.Any);
    }

    public async Task<byte[]> GetUserPhotoAsync(int tenant, Guid id)
    {
        var photo = _cache.Get<byte[]>(UserServiceCache.GetUserPhotoCacheKey(tenant, id));
        if (photo == null)
        {
            photo = await _service.GetUserPhotoAsync(tenant, id);
            _cache.Insert(UserServiceCache.GetUserPhotoCacheKey(tenant, id), photo, _photoExpiration);
        }

        return photo;
    }

    public async Task SetUserPhotoAsync(int tenant, Guid id, byte[] photo)
    {
        await _service.SetUserPhotoAsync(tenant, id, photo);
        await _cacheUserPhotoItem.PublishAsync(new UserPhotoCacheItem { Key = UserServiceCache.GetUserPhotoCacheKey(tenant, id) }, CacheNotifyAction.Remove);
        await _cacheUserInfoItem.PublishAsync(new UserInfoCacheItem { Id = id.ToString(), Tenant = tenant }, CacheNotifyAction.Any);
    }

    public async Task<DateTime> GetUserPasswordStampAsync(int tenant, Guid id)
    {
        return await _service.GetUserPasswordStampAsync(tenant, id);
    }

    public async Task SetUserPasswordHashAsync(int tenant, Guid id, string passwordHash)
    {
        await _service.SetUserPasswordHashAsync(tenant, id, passwordHash);
    }

    public async Task<Group> GetGroupAsync(int tenant, Guid id)
    {
        var key = UserServiceCache.GetGroupCacheKey(tenant, id);
        var group = _cache.Get<Group>(key);

        if (group == null)
        {
            group = await _service.GetGroupAsync(tenant, id);

            if (group != null)
            {
                _cache.Insert(key, group, _cacheExpiration);
            }
        }

        return group;
    }

    public async Task<Group> SaveGroupAsync(int tenant, Group group)
    {
        group = await _service.SaveGroupAsync(tenant, group);
        await _cacheGroupCacheItem.PublishAsync(new GroupCacheItem { Id = group.Id.ToString(), Tenant = tenant }, CacheNotifyAction.Any);

        return group;
    }

    public async Task RemoveGroupAsync(int tenant, Guid id)
    {
        await _service.RemoveGroupAsync(tenant, id);
        await _cacheGroupCacheItem.PublishAsync(new GroupCacheItem { Id = id.ToString(), Tenant = tenant }, CacheNotifyAction.Any);
    }


    public async Task<IDictionary<string, UserGroupRef>> GetUserGroupRefsAsync(int tenant)
    {
        var key = UserServiceCache.GetRefCacheKey(tenant);
        if (_cache.Get<UserGroupRefStore>(key) is not IDictionary<string, UserGroupRef> refs)
        {
            refs = await _service.GetUserGroupRefsAsync(tenant);
            _cache.Insert(key, new UserGroupRefStore(refs), _cacheExpiration);
        }

        return refs;
    }

    public async Task<UserGroupRef> GetUserGroupRefAsync(int tenant, Guid groupId, UserGroupRefType refType)
    {
        var key = UserServiceCache.GetRefCacheKey(tenant, groupId, refType);
        var groupRef = _cache.Get<UserGroupRef>(key);

        if (groupRef == null)
        {
            groupRef = await _service.GetUserGroupRefAsync(tenant, groupId, refType);

            if (groupRef != null)
            {
                _cache.Insert(key, groupRef, _cacheExpiration);
            }
        }

        return groupRef;
    }

    public async Task<UserGroupRef> SaveUserGroupRefAsync(int tenant, UserGroupRef r)
    {
        r = await _service.SaveUserGroupRefAsync(tenant, r);
        await _cacheUserGroupRefItem.PublishAsync(r, CacheNotifyAction.InsertOrUpdate);

        return r;
    }

    public async Task RemoveUserGroupRefAsync(int tenant, Guid userId, Guid groupId, UserGroupRefType refType)
    {
        await _service.RemoveUserGroupRefAsync(tenant, userId, groupId, refType);

        var r = new UserGroupRef(userId, groupId, refType) { TenantId = tenant, Removed = true };
        await _cacheUserGroupRefItem.PublishAsync(r, CacheNotifyAction.Remove);
    }


    public async Task<IEnumerable<UserInfo>> GetUsersAsync(int tenant)
    {
        var key = UserServiceCache.GetUserCacheKey(tenant);
        var users = _cache.Get<IEnumerable<UserInfo>>(key);
        if (users == null)
        {
            users = await _service.GetUsersAsync(tenant);

            _cache.Insert(key, users, _cacheExpiration);
        }

        return users;
    }

    public async Task<IEnumerable<Group>> GetGroupsAsync(int tenant)
    {
        var key = UserServiceCache.GetGroupCacheKey(tenant);
        var groups = _cache.Get<IEnumerable<Group>>(key);
        if (groups == null)
        {
            groups = await _service.GetGroupsAsync(tenant);
            _cache.Insert(key, groups, _cacheExpiration);
        }

        return groups;
    }

    public IAsyncEnumerable<Group> GetGroupsAsync(int tenant, string text, Guid userId, bool manager, GroupSortType sortBy, bool sortOrderAsc, int offset = 0, int count = -1)
    {
        return _service.GetGroupsAsync(tenant, text, userId, manager, sortBy, sortOrderAsc, offset, count);
    }

    public Task<int> GetGroupsCountAsync(int tenant, string text, Guid userId, bool manager)
    {
        return _service.GetGroupsCountAsync(tenant, text, userId, manager);
    }

    public void InvalidateCache()
    {
    }

    public async Task<UserInfo> GetUserAsync(int tenant, Guid id, Expression<Func<User, UserInfo>> exp)
    {
        if (exp == null)
        {
            return await GetUserAsync(tenant, id);
        }

        return await _service.GetUserAsync(tenant, id, exp);
    }

    public async Task<IEnumerable<string>> GetDavUserEmailsAsync(int tenant)
    {
        return await _service.GetDavUserEmailsAsync(tenant);
    }
}
