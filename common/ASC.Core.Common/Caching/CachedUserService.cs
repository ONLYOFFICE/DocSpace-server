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

namespace ASC.Core.Caching;


[Scope(typeof(IUserService))]
public class CachedUserService : IUserService
{
    private readonly EFUserService _service;
    private readonly IFusionCache _cache;

    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _photoExpiration;

    public CachedUserService(
        EFUserService service,
        IFusionCacheProvider cacheProvider)
    {
        _service = service;
        _cache = cacheProvider.GetMemoryCache();
        _cacheExpiration = TimeSpan.FromMinutes(20);
        _photoExpiration = TimeSpan.FromMinutes(10);
    }

    private const string Users = "users";
    private const string Refs = "refs";
    private const string Groups = "groups";
    private const string Relations = "relations";

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

    public static string GetRelationCacheKey(int tenant, string sourceUserId)
    {
        return tenant + Relations + sourceUserId;
    }

    public Task<int> GetUsersCountAsync(UserQueryFilter filter)
    {
        return _service.GetUsersCountAsync(filter);
    }

    public IAsyncEnumerable<UserInfo> GetUsers(UserQueryFilter filter)
    {
        return _service.GetUsers(filter);
    }

    public async Task<UserInfo> GetUserAsync(int tenant, Guid id)
    {
        var key = GetUserCacheKey(tenant, id);
        var user = await _cache.GetOrSetAsync<UserInfo>(key, async (ctx, token) =>
        {
            var user = await _service.GetUserAsync(tenant, id);

            return ctx.Modified(user);
        }, _cacheExpiration, [CacheExtention.GetUserTag(tenant, id)]);

        return user;
    }

    public UserInfo GetUser(int tenant, Guid id)
    {
        var key = GetUserCacheKey(tenant, id);
        var user = _cache.GetOrSet<UserInfo>(key, (ctx, token) =>
        {
            var user = _service.GetUser(tenant, id);

            return ctx.Modified(user, lastModified: DateTime.UtcNow);
        }, _cacheExpiration, [CacheExtention.GetUserTag(tenant, id)]);

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
        var tag = CacheExtention.GetUserTag(tenant, user.Id);

        user = await _service.SaveUserAsync(tenant, user);

        await _cache.RemoveByTagAsync(tag);

        return user;
    }

    public async Task RemoveUserAsync(int tenant, Guid id, bool immediate = false)
    {
        await _service.RemoveUserAsync(tenant, id, immediate);

        var tag = CacheExtention.GetUserTag(tenant, id);
        await _cache.RemoveByTagAsync(tag);
    }

    public async Task<byte[]> GetUserPhotoAsync(int tenant, Guid id)
    {
        var key = GetUserPhotoCacheKey(tenant, id);
        var photo = await _cache.GetOrSetAsync<byte[]>(key, async (ctx, token) =>
        {
            var photo = await _service.GetUserPhotoAsync(tenant, id);

            return ctx.Modified(photo);
        }, _photoExpiration, [CacheExtention.GetUserPhotoTag(tenant, id)]);
        return photo;
    }

    public async Task SetUserPhotoAsync(int tenant, Guid id, byte[] photo)
    {
        await _service.SetUserPhotoAsync(tenant, id, photo);

        var tag = CacheExtention.GetUserPhotoTag(tenant, id);
        await _cache.RemoveAsync(tag);
    }

    public async Task SaveUsersRelationAsync(int tenantId, Guid sourceUserId, Guid targetUserId)
    {
        await _service.SaveUsersRelationAsync(tenantId, sourceUserId, targetUserId);

        var tag = CacheExtention.GetRelationTag(tenantId, sourceUserId);
        await _cache.RemoveByTagAsync(tag);
    }

    public async Task<Dictionary<Guid, UserRelation>> GetUserRelationsAsync(int tenantId, Guid sourceUserId)
    {
        var key = GetRelationCacheKey(tenantId, sourceUserId.ToString());

        var relations = await _cache.GetOrSetAsync<Dictionary<Guid, UserRelation>>(key, async (ctx, token) =>
        {
            var relations = await _service.GetUserRelationsAsync(tenantId, sourceUserId);

            return ctx.Modified(relations);
        }, _cacheExpiration, [CacheExtention.GetRelationTag(tenantId, sourceUserId)]);

        return relations;
    }

    public async Task<Dictionary<Guid, UserRelation>> GetUserRelationsByTargetAsync(int tenantId, Guid targetUserId)
    {
        var relations = await _service.GetUserRelationsByTargetAsync(tenantId, targetUserId);

        return relations;
    }

    public async Task DeleteUserRelationAsync(int tenantId, Guid sourceUserId, Guid targetUserId)
    {
        await _service.DeleteUserRelationAsync(tenantId, sourceUserId, targetUserId);

        var tag = CacheExtention.GetRelationTag(tenantId, sourceUserId);
        await _cache.RemoveByTagAsync(tag);
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
        var key = GetGroupCacheKey(tenant, id);

        var group = await _cache.GetOrSetAsync<Group>(key, async (ctx, token) =>
        {
            var group = await _service.GetGroupAsync(tenant, id);

            return ctx.Modified(group);
        }, _cacheExpiration, [CacheExtention.GetGroupTag(tenant, id)]);

        return group;
    }

    public async Task<Group> SaveGroupAsync(int tenant, Group group)
    {
        var tag = CacheExtention.GetGroupTag(tenant, group.Id);

        group = await _service.SaveGroupAsync(tenant, group);

        await _cache.RemoveByTagAsync(tag);

        return group;
    }

    public async Task RemoveGroupAsync(int tenant, Guid id)
    {
        await _service.RemoveGroupAsync(tenant, id);

        var tag = CacheExtention.GetGroupTag(tenant, id);
        await _cache.RemoveByTagAsync(tag);
    }


    public async Task<IDictionary<string, UserGroupRef>> GetUserGroupRefsAsync(int tenant)
    {
        var key = GetRefCacheKey(tenant);

        var refs = await _cache.GetOrSetAsync<UserGroupRefStore>(key, async (ctx, token) =>
        {
            var refs = await _service.GetUserGroupRefsAsync(tenant);

            ctx.Tags = [CacheExtention.GetGroupRefsTag(tenant)];

            return ctx.Modified(new UserGroupRefStore(refs));
        }, _cacheExpiration);

        return refs;
    }

    public async Task<UserGroupRef> GetUserGroupRefAsync(int tenant, Guid groupId, UserGroupRefType refType)
    {
        var key = GetRefCacheKey(tenant, groupId, refType);

        var groupRef = await _cache.GetOrSetAsync<UserGroupRef>(key, async (ctx, token) =>
        {
            var groupRef = await _service.GetUserGroupRefAsync(tenant, groupId, refType);

            ctx.Tags = [CacheExtention.GetGroupRefsTag(tenant)];

            return ctx.Modified(groupRef, lastModified: DateTime.UtcNow);
        }, _cacheExpiration);

        return groupRef;
    }

    public async Task<UserGroupRef> SaveUserGroupRefAsync(int tenant, UserGroupRef r)
    {
        r = await _service.SaveUserGroupRefAsync(tenant, r);

        var tag = CacheExtention.GetGroupRefsTag(tenant);
        await _cache.RemoveByTagAsync(tag);

        return r;
    }

    public async Task RemoveUserGroupRefAsync(int tenant, Guid userId, Guid groupId, UserGroupRefType refType)
    {
        await _service.RemoveUserGroupRefAsync(tenant, userId, groupId, refType);

        var r = new UserGroupRef(userId, groupId, refType) { TenantId = tenant, Removed = true };

        var tag = CacheExtention.GetGroupRefsTag(tenant);
        await _cache.RemoveByTagAsync(tag);
    }


    public async Task<IEnumerable<UserInfo>> GetUsersAsync(int tenant)
    {
        var key = GetUserCacheKey(tenant);

        var users = await _cache.GetOrSetAsync<IEnumerable<UserInfo>>(key, async (ctx, token) =>
        {
            var users = await _service.GetUsersAsync(tenant);

            ctx.Tags = [CacheExtention.GetUserTag(tenant, Guid.Empty), .. users.Select(u => CacheExtention.GetUserTag(tenant, u.Id))];

            return ctx.Modified(users);
        }, _cacheExpiration);

        return users;
    }

    public async Task<IEnumerable<Group>> GetGroupsAsync(int tenant)
    {
        var key = GetGroupCacheKey(tenant);

        var groups = await _cache.GetOrSetAsync<IEnumerable<Group>>(key, async (ctx, token) =>
        {
            var groups = await _service.GetGroupsAsync(tenant);
            ctx.Tags = [CacheExtention.GetGroupTag(tenant, Guid.Empty), .. groups.Select(g => CacheExtention.GetGroupTag(tenant, g.Id))];

            return ctx.Modified(groups);
        }, _cacheExpiration);

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

    public async Task<InvitationLink> CreateInvitationLinkAsync(int tenantId, EmployeeType employeeType, DateTime expiration, int maxUseCount)
    {
        return await _service.CreateInvitationLinkAsync(tenantId, employeeType, expiration, maxUseCount);
    }

    public async Task<InvitationLink> GetInvitationLinkAsync(int tenantId, Guid id)
    {
        return await _service.GetInvitationLinkAsync(tenantId, id);
    }

    public async Task<InvitationLink> GetInvitationLinkAsync(int tenantId, EmployeeType employeeType)
    {
        return await _service.GetInvitationLinkAsync(tenantId, employeeType);
    }

    public async Task<List<InvitationLink>> GetInvitationLinksAsync(int tenantId)
    {
        return await _service.GetInvitationLinksAsync(tenantId);
    }

    public async Task UpdateInvitationLinkAsync(int tenantId, Guid id, DateTime expiration, int maxUseCount)
    {
        await _service.UpdateInvitationLinkAsync(tenantId, id, expiration, maxUseCount);
    }

    public async Task IncreaseInvitationLinkUsageAsync(int tenantId, Guid id)
    {
        await _service.IncreaseInvitationLinkUsageAsync(tenantId, id);
    }

    public async Task DeleteInvitationLinkAsync(int tenantId, Guid id)
    {
        await _service.DeleteInvitationLinkAsync(tenantId, id);
    }
}