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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Core.Data;

[Scope]
public class EFUserService(IDbContextFactory<UserDbContext> dbContextFactory,
        MachinePseudoKeys machinePseudoKeys,
        IMapper mapper)
    : IUserService
{
    public async Task<Group> GetGroupAsync(int tenant, Guid id)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return await GetGroupQuery(userDbContext, tenant)
            .Where(r => r.Id == id)
            .ProjectTo<Group>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Group>> GetGroupsAsync(int tenant)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return await GetGroupQuery(userDbContext, tenant)
            .ProjectTo<Group>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async IAsyncEnumerable<Group> GetGroupsAsync(int tenant, string text, Guid userId, bool manager, GroupSortType sortBy, bool sortOrderAsc, int offset = 0, int count = -1)
    {
        if (count == 0)
        {
            yield break;
        }
        
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = userDbContext.Groups.Where(g => g.TenantId == tenant && !g.Removed);

        q = BuildTextSearch(text, q);

        if (userId != Guid.Empty)
        {
            var q1 = BuildUserGroupSearch(userId, manager, q, userDbContext);

            if (sortBy == GroupSortType.Manager)
            {
                var q2 = q1.Join(userDbContext.UserGroups, group => group.Id, userGroup => userGroup.UserGroupId,
                    (group, userGroup) => new { group, userGroup })
                    .Where(r => !r.userGroup.Removed && r.userGroup.RefType == UserGroupRefType.Manager)
                    .Join(userDbContext.Users, record => record.userGroup.Userid, 
                        user => user.Id, (record, user) => new { record.group, user });
                
                q = (sortOrderAsc ? q2.OrderBy(r => r.user.FirstName) : q2.OrderByDescending(r => r.user.FirstName))
                    .Select(r => r.group);
            }
            else
            { 
                q = sortOrderAsc ? q1.OrderBy(g => g.Name) : q1.OrderByDescending(g => g.Name);
            }
        }
        else
        {
            if (sortBy == GroupSortType.Manager)
            {
                var q1 = from dbGroup in q
                    join userGroup in userDbContext.UserGroups on dbGroup.Id equals userGroup.UserGroupId
                        into userGroups
                    from userGroup in userGroups.DefaultIfEmpty()
                    where userGroup == null || (userGroup.RefType == UserGroupRefType.Manager && !userGroup.Removed)
                    join user in userDbContext.Users on userGroup.Userid equals user.Id
                        into users
                    from user in users.DefaultIfEmpty()
                    select new { dbGroup, user };
            
                q = (sortOrderAsc 
                        ? q1.OrderBy(r => r.user == null).ThenBy(r => r.user.FirstName) 
                        : q1.OrderBy(r => r.user == null).ThenByDescending(r => r.user.FirstName))
                    .Select(r => r.dbGroup);
            }
            else
            {
                q = sortOrderAsc ? q.OrderBy(g => g.Name) : q.OrderByDescending(g => g.Name);
            }
        }

        if (offset > 0)
        {
            q = q.Skip(offset);
        }

        if (count > 0)
        {
            q = q.Take(count);
        }
        
        await foreach (var group in q.ProjectTo<Group>(mapper.ConfigurationProvider).ToAsyncEnumerable())
        {
            yield return group;
        }
    }

    public async Task<int> GetGroupsCountAsync(int tenant, string text, Guid userId, bool manager)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = userDbContext.Groups.Where(g => g.TenantId == tenant && !g.Removed);

        q = BuildTextSearch(text, q);

        if (userId == Guid.Empty)
        {
            return await q.CountAsync();
        }

        q = BuildUserGroupSearch(userId, manager, q, userDbContext);

        return await q.CountAsync();
    }

    public async Task<UserInfo> GetUserAsync(int tenant, Guid id)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return await GetUserQuery(userDbContext, tenant)
            .Where(r => r.Id == id)
            .ProjectTo<UserInfo>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public UserInfo GetUser(int tenant, Guid id)
    {
        using var userDbContext = dbContextFactory.CreateDbContext();
        return GetUserQuery(userDbContext, tenant)
            .Where(r => r.Id == id)
            .ProjectTo<UserInfo>(mapper.ConfigurationProvider)
            .FirstOrDefault();
    }

    public async Task<UserInfo> GetUserAsync(int tenant, string email)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return await GetUserQuery(userDbContext, tenant)
            .ProjectTo<UserInfo>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(r => r.Email == email && !r.Removed);
    }

    public async Task<UserInfo> GetUserByUserName(int tenant, string userName)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return await GetUserQuery(userDbContext, tenant)
            .ProjectTo<UserInfo>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(r => r.UserName == userName && !r.Removed);
    }

    public async Task<UserInfo> GetUserByPasswordHashAsync(int tenant, string login, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrEmpty(login);

        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        if (Guid.TryParse(login, out var userId))
        {
            var pwdHash = GetPasswordHash(userId, passwordHash);

            var q = GetUserQuery(userDbContext, tenant)
                .Where(r => !r.Removed)
                .Where(r => r.Id == userId)
                .Join(userDbContext.UserSecurity, r => r.Id, r => r.UserId, (user, security) => new DbUserSecurity
                {
                    User = user,
                    UserSecurity = security
                })
                .Where(r => r.UserSecurity.PwdHash == pwdHash);

            if (tenant != Tenant.DefaultTenant)
            {
                q = q.Where(r => r.UserSecurity.TenantId == tenant);
            }

            return await q.Select(r => r.User)
                .ProjectTo<UserInfo>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }
        else
        {
            var q = GetUserQuery(userDbContext, tenant)
                .Where(r => !r.Removed)
                .Where(r => r.Email == login);

            var users = await q.ProjectTo<UserInfo>(mapper.ConfigurationProvider).ToListAsync();
            foreach (var user in users)
            {
                var pwdHash = GetPasswordHash(user.Id, passwordHash);

                var any = await userDbContext.UserSecurity
                    .AnyAsync(r => r.UserId == user.Id && (r.PwdHash == pwdHash));

                if (any)
                {
                    return user;
                }
            }

            return null;
        }
    }

    public async Task<IEnumerable<UserInfo>> GetUsersAllTenantsAsync(IEnumerable<Guid> userIds)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        var q = userDbContext.Users
            .Where(r => userIds.Contains(r.Id))
            .Where(r => !r.Removed);

        return await q.ProjectTo<UserInfo>(mapper.ConfigurationProvider).ToListAsync();
    }

    public UserGroupRef GetUserGroupRef(int tenant, Guid groupId, UserGroupRefType refType)
    {
        using var userDbContext = dbContextFactory.CreateDbContext();
        
        return GetUserGroupRefQuery(tenant, groupId, refType, userDbContext).SingleOrDefault();
    }

    public async Task<UserGroupRef> GetUserGroupRefAsync(int tenant, Guid groupId, UserGroupRefType refType)
    {
        await using var userDbContext = dbContextFactory.CreateDbContext();
        
        return await GetUserGroupRefQuery(tenant, groupId, refType, userDbContext).SingleOrDefaultAsync();
    }

    private IQueryable<UserGroupRef> GetUserGroupRefQuery(int tenant, Guid groupId, UserGroupRefType refType, UserDbContext userDbContext)
    {
        IQueryable<UserGroup> q = userDbContext.UserGroups;

        if (tenant != Tenant.DefaultTenant)
        {
            q = q.Where(r => r.TenantId == tenant);
        }
        
        return q.Where(r => r.UserGroupId == groupId && r.RefType == refType && !r.Removed)
            .ProjectTo<UserGroupRef>(mapper.ConfigurationProvider);
    }

    public async Task<IDictionary<string, UserGroupRef>> GetUserGroupRefsAsync(int tenant)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        IQueryable<UserGroup> q = userDbContext.UserGroups;

        if (tenant != Tenant.DefaultTenant)
        {
            q = q.Where(r => r.TenantId == tenant);
        }

        return await q.ProjectTo<UserGroupRef>(mapper.ConfigurationProvider).ToDictionaryAsync(r => r.CreateKey(), r => r);
    }

    public IDictionary<string, UserGroupRef> GetUserGroupRefs(int tenant)
    {
        using var userDbContext = dbContextFactory.CreateDbContext();
        IQueryable<UserGroup> q = userDbContext.UserGroups;

        if (tenant != Tenant.DefaultTenant)
        {
            q = q.Where(r => r.TenantId == tenant);
        }

        return q.ProjectTo<UserGroupRef>(mapper.ConfigurationProvider)
            .AsEnumerable().ToDictionary(r => r.CreateKey(), r => r);
    }

    public async Task<DateTime> GetUserPasswordStampAsync(int tenant, Guid id)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var stamp = await Queries.LastModifiedAsync(userDbContext, tenant, id);

        return stamp ?? DateTime.MinValue;
    }

    public async Task<byte[]> GetUserPhotoAsync(int tenant, Guid id)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var photo = await Queries.PhotoAsync(userDbContext, tenant, id);

        return photo ?? Array.Empty<byte>();
    }

    public async Task<IEnumerable<UserInfo>> GetUsersAsync(int tenant)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return await GetUserQuery(userDbContext, tenant)
            .ProjectTo<UserInfo>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<int> GetUsersCountAsync(
        int tenant,
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
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = GetUserQuery(userDbContext, tenant);

        q = GetUserQueryForFilter(userDbContext, q, isDocSpaceAdmin, employeeStatus, includeGroups, excludeGroups, combinedGroups, activationStatus, accountLoginType, text, withoutGroup);

        return await q.CountAsync();
    }

    public async IAsyncEnumerable<UserInfo> GetUsers(
        int tenant,
        bool isDocSpaceAdmin,
        EmployeeStatus? employeeStatus,
        List<List<Guid>> includeGroups,
        List<Guid> excludeGroups,
        List<Tuple<List<List<Guid>>, List<Guid>>> combinedGroups,
        EmployeeActivationStatus? activationStatus,
        AccountLoginType? accountLoginType,
        string text,
        bool withoutGroup,
        Guid ownerId,
        UserSortType sortBy,
        bool sortOrderAsc,
        long limit,
        long offset)
    {
        if (limit <= 0)
        {
            yield break;
        }

        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = GetUserQuery(userDbContext, tenant);

        q = GetUserQueryForFilter(userDbContext, q, isDocSpaceAdmin, employeeStatus, includeGroups, excludeGroups, combinedGroups, activationStatus, accountLoginType, text, withoutGroup);

        switch (sortBy)
        {
            case UserSortType.Type:
                {
                    var q1 = (from user in q
                        join userGroup in userDbContext.UserGroups.Where(g =>
                            !g.Removed && (g.UserGroupId == Constants.GroupAdmin.ID || g.UserGroupId == Constants.GroupUser.ID ||
                                           g.UserGroupId == Constants.GroupCollaborator.ID)) on user.Id equals userGroup.Userid into joinedGroup
                        from @group in joinedGroup.DefaultIfEmpty()
                        select new UserWithGroup { User = user, Group = @group });

                    Expression<Func<UserWithGroup, int>> orderByUserType = u => 
                        u.User.Id == ownerId ? 0 :
                        u.Group == null ? 2 : 
                        u.Group.UserGroupId == Constants.GroupAdmin.ID ? 1 : 
                        u.Group.UserGroupId == Constants.GroupCollaborator.ID ? 3 : 4;
                
                    q = (sortOrderAsc ? q1.OrderBy(orderByUserType) : q1.OrderByDescending(orderByUserType)).Select(r => r.User);
                    break;
                }
            case UserSortType.Department:
                {
                    var q1 = q.Select(u => new
                    {
                        user = u,
                        groupsCount = userDbContext.UserGroups.Count(g =>
                            g.TenantId == tenant && g.Userid == u.Id && !g.Removed && g.RefType == UserGroupRefType.Contains && 
                            !Constants.SystemGroups.Select(sg => sg.ID).Contains(g.UserGroupId))
                    });

                    q = (sortOrderAsc 
                            ? q1.OrderBy(r => r.groupsCount).ThenBy(r => r.user.FirstName) 
                            : q1.OrderByDescending(r => r.groupsCount)).ThenByDescending(r => r.user.FirstName)
                        .Select(r => r.user);
                    break;
                }
            case UserSortType.Email:
                q = (sortOrderAsc ? q.OrderBy(u => u.Email) : q.OrderByDescending(u => u.Email));
                break;
            case UserSortType.FirstName:
            default:
                q = sortOrderAsc 
                    ? q.OrderBy(r => r.ActivationStatus).ThenBy(u => u.FirstName) 
                    : q.OrderBy(r => r.ActivationStatus).ThenByDescending(u => u.FirstName);
                break;
        }

        if (offset > 0)
        {
            q = q.Skip((int)offset);
        }

        q = q.Take((int)limit);

        await foreach (var user in q.ToAsyncEnumerable())
        {
            yield return mapper.Map<User, UserInfo>(user);
        }
    }

    public async Task<IEnumerable<int>> GetTenantsWithFeedsAsync(DateTime from)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        return await Queries.TenantIdsAsync(userDbContext, from).ToListAsync();
    }

    public async Task RemoveGroupAsync(int tenant, Guid id)
    {
        await RemoveGroupAsync(tenant, id, false);
    }

    private async Task RemoveGroupAsync(int tenant, Guid id, bool immediate)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        var ids = await CollectGroupChildAsync(userDbContext, tenant, id);
        var stringIds = ids.Select(r => r.ToString()).ToList();

        var strategy = userDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
            await using var tx = await filesDbContext.Database.BeginTransactionAsync();

            await Queries.DeleteAclByIdsAsync(userDbContext, tenant, ids);
            await Queries.DeleteSubscriptionsByIdsAsync(userDbContext, tenant, stringIds);
            await Queries.DeleteDbSubscriptionMethodsByIdsAsync(userDbContext, tenant, stringIds);

            if (immediate)
            {
                await Queries.DeleteUserGroupsByIdsAsync(userDbContext, tenant, ids);
                await Queries.DeleteDbGroupsAsync(userDbContext, tenant, ids);
            }
            else
            {
                await Queries.UpdateUserGroupsByIdsAsync(userDbContext, tenant, ids);
                await Queries.UpdateDbGroupsAsync(userDbContext, tenant, ids);
            }

            await tx.CommitAsync();
        });
    }

    public async Task RemoveUserAsync(int tenant, Guid id, bool immediate = false)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var strategy = userDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await using var tr = await dbContext.Database.BeginTransactionAsync();

            await Queries.DeleteAclAsync(dbContext, tenant, id);
            await Queries.DeleteSubscriptionsAsync(dbContext, tenant, id.ToString());
            await Queries.DeleteDbSubscriptionMethodsAsync(dbContext, tenant, id.ToString());
            await Queries.DeleteUserPhotosAsync(dbContext, tenant, id);
            await Queries.DeleteAccountLinksAsync(dbContext, id.ToString());

            if (immediate)
            {
                await Queries.DeleteUserGroupsAsync(dbContext, tenant, id);
                await Queries.DeleteUsersAsync(dbContext, tenant, id);
                await Queries.DeleteUserSecuritiesAsync(dbContext, tenant, id);
            }
            else
            {
                await Queries.UpdateUserGroupsAsync(dbContext, tenant, id);
                await Queries.UpdateUsersAsync(dbContext, tenant, id);
            }
            await tr.CommitAsync();
        });
    }

    public async Task RemoveUserGroupRefAsync(int tenant, Guid userId, Guid groupId, UserGroupRefType refType)
    {
        await RemoveUserGroupRefAsync(tenant, userId, groupId, refType, false);
    }

    private async Task RemoveUserGroupRefAsync(int tenant, Guid userId, Guid groupId, UserGroupRefType refType, bool immediate)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync(); 
        var strategy = userDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await using var tr = await dbContext.Database.BeginTransactionAsync();
            if (immediate)
            {
                await Queries.DeleteUserGroupsByGroupIdAsync(dbContext, tenant, userId, groupId, refType);
            }
            else
            {
                await Queries.UpdateUserGroupsByGroupIdAsync(dbContext, tenant, userId, groupId, refType);
            }

            var user = await Queries.UserAsync(dbContext, tenant, userId);
            user.LastModified = DateTime.UtcNow;
            dbContext.Update(user);

            await dbContext.SaveChangesAsync();
            await tr.CommitAsync();
        });
    }

    public async Task<Group> SaveGroupAsync(int tenant, Group group)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (group.Id == Guid.Empty)
        {
            group.Id = Guid.NewGuid();
        }

        group.LastModified = DateTime.UtcNow;
        group.TenantId = tenant;

        var dbGroup = mapper.Map<Group, DbGroup>(group);

        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        await userDbContext.AddOrUpdateAsync(q => q.Groups, dbGroup);
        await userDbContext.SaveChangesAsync();

        return group;
    }

    public async Task<UserInfo> SaveUserAsync(int tenant, UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(user.UserName))
        {
            throw new ArgumentNullException(nameof(user.UserName));
        }

        if (user.Id == Guid.Empty)
        {
            user.Id = Guid.NewGuid();
        }

        if (user.CreateDate == default)
        {
            user.CreateDate = DateTime.UtcNow;
        }

        user.LastModified = DateTime.UtcNow;
        user.TenantId = tenant;
        user.UserName = user.UserName.Trim();
        user.Email = user.Email.Trim();

        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var any = await Queries.AnyUsersAsync(userDbContext, tenant, user.UserName, user.Id);

        if (any)
        {
            throw new ArgumentException($"Duplicate {nameof(user.UserName)}");
        }

        any = await Queries.AnyUsersByEmailAsync(userDbContext, tenant, user.Email, user.Id);

        if (any)
        {
            throw new ArgumentException($"Duplicate {nameof(user.Email)}");
        }

        await userDbContext.AddOrUpdateAsync(q => q.Users, mapper.Map<UserInfo, User>(user));
        await userDbContext.SaveChangesAsync();

        return user;
    }

    public async Task<UserGroupRef> SaveUserGroupRefAsync(int tenant, UserGroupRef userGroupRef)
    {
        ArgumentNullException.ThrowIfNull(userGroupRef);

        userGroupRef.LastModified = DateTime.UtcNow;
        userGroupRef.TenantId = tenant;

        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var user = await Queries.FirstOrDefaultUserAsync(userDbContext, tenant, userGroupRef.UserId);
        if (user != null)
        {
            user.LastModified = userGroupRef.LastModified;
            await userDbContext.AddOrUpdateAsync(q => q.UserGroups, mapper.Map<UserGroupRef, UserGroup>(userGroupRef));
            await userDbContext.SaveChangesAsync();
        }

        return userGroupRef;
    }

    public async Task SetUserPasswordHashAsync(int tenant, Guid id, string passwordHash)
    {
        var h1 = GetPasswordHash(id, passwordHash);

        var us = new UserSecurity
        {
            TenantId = tenant,
            UserId = id,
            PwdHash = h1,
            LastModified = DateTime.UtcNow
        };

        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        await userDbContext.AddOrUpdateAsync(q => q.UserSecurity, us);
        await userDbContext.SaveChangesAsync();
    }

    public async Task SetUserPhotoAsync(int tenant, Guid id, byte[] photo)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var userPhoto = await Queries.UserPhotoAsync(userDbContext, tenant, id);
        if (photo != null && photo.Length != 0)
        {
            if (userPhoto == null)
            {
                userPhoto = new UserPhoto
                {
                    TenantId = tenant,
                    UserId = id,
                    Photo = photo
                };
            }
            else
            {
                userPhoto.Photo = photo;
            }

            await userDbContext.AddOrUpdateAsync(q => q.Photos, userPhoto);
            
            var userEntity = new User
            {
                Id = id,
                LastModified = DateTime.UtcNow,
                TenantId = tenant
            };

            userDbContext.Users.Attach(userEntity);
            userDbContext.Entry(userEntity).Property(x => x.LastModified).IsModified = true;
        }
        else if (userPhoto != null)
        {
            userDbContext.Photos.Remove(userPhoto);
        }

        await userDbContext.SaveChangesAsync();
    }

    private IQueryable<User> GetUserQuery(UserDbContext userDbContext, int tenant)
    {
        var q = userDbContext.Users.AsQueryable();
        var where = false;

        if (tenant != Tenant.DefaultTenant)
        {
            q = q.Where(r => r.TenantId == tenant);
            where = true;
        }

        if (!where)
        {
            q = q.Where(r => 1 == 0);
        }

        return q;
    }

    private IQueryable<DbGroup> GetGroupQuery(UserDbContext userDbContext, int tenant)
    {
        var q = userDbContext.Groups.Where(r => true);

        if (tenant != Tenant.DefaultTenant)
        {
            q = q.Where(r => r.TenantId == tenant);
        }

        return q;
    }

    private IQueryable<User> GetUserQueryForFilter(
        UserDbContext userDbContext,
        IQueryable<User> q,
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
        q = q.Where(r => !r.Removed);

        if (includeGroups is { Count: > 0 } || excludeGroups is { Count: > 0 })
        {
            if (includeGroups is { Count: > 0 })
            {
                foreach (var ig in includeGroups)
                {
                    q = q.Where(r => userDbContext.UserGroups.Any(a => !a.Removed && a.TenantId == r.TenantId && a.Userid == r.Id && ig.Any(id => id == a.UserGroupId)));
                }
            }

            if (excludeGroups is { Count: > 0 })
            {
                foreach (var eg in excludeGroups)
                {
                    q = q.Where(r => !userDbContext.UserGroups.Any(a => !a.Removed && a.TenantId == r.TenantId && a.Userid == r.Id && a.UserGroupId == eg));
                }
            }
        }
        else if (combinedGroups != null && combinedGroups.Count != 0)
        {
            Expression<Func<User, bool>> a = r => false;

            foreach (var (cgIncludeGroups, cgExcludeGroups) in combinedGroups)
            {
                Expression<Func<User, bool>> b = r => true;

                if (cgIncludeGroups is { Count: > 0 })
                {
                    foreach (var ig in cgIncludeGroups)
                    {
                        b = b.And(r => userDbContext.UserGroups.Any(user => !user.Removed && user.TenantId == r.TenantId && user.Userid == r.Id && ig.Any(ug => ug == user.UserGroupId)));
                    }
                }

                if (cgExcludeGroups is { Count: > 0 })
                {
                    foreach (var eg in cgExcludeGroups)
                    {
                        b = b.And(r => !userDbContext.UserGroups.Any(ug => !ug.Removed && ug.TenantId == r.TenantId && ug.Userid == r.Id && ug.UserGroupId == eg));
                    }
                }

                a = a.Or(b);
            }

            q = q.Where(a);
        }

        if (withoutGroup)
        {
            q = from user in q
                join userGroup in userDbContext.UserGroups.Where(g => 
                        !g.Removed && !Constants.SystemGroups.Select(gi => gi.ID).Contains(g.UserGroupId)) 
                    on user.Id equals userGroup.Userid into joinedSet
                from @group in joinedSet.DefaultIfEmpty()
                where @group == null
                select user;
        }

        if (!isDocSpaceAdmin && employeeStatus == null)
        {
            q = q.Where(r => r.Status != EmployeeStatus.Terminated);
        }

        if (employeeStatus != null)
        {
            switch (employeeStatus)
            {
                case EmployeeStatus.LeaveOfAbsence:
                case EmployeeStatus.Terminated:
                    q = isDocSpaceAdmin ? q.Where(u => u.Status == EmployeeStatus.Terminated) : q.Where(u => false);
                    break;
                case EmployeeStatus.All:
                    if (!isDocSpaceAdmin)
                    {
                        q = q.Where(r => r.Status != EmployeeStatus.Terminated);
                    }

                    break;
                case EmployeeStatus.Default:
                case EmployeeStatus.Active:
                    q = q.Where(u => u.Status == EmployeeStatus.Active);
                    break;
            }
        }

        if (activationStatus != null)
        {
            q = q.Where(r => r.ActivationStatus == activationStatus.Value);
        }

        if (!string.IsNullOrEmpty(text))
        {
            q = q.Where(
                u => u.FirstName.Contains(text) ||
                u.LastName.Contains(text) ||
                u.Title.Contains(text) ||
                u.Location.Contains(text) ||
                u.Email.Contains(text));
        }

        q = accountLoginType switch
        {
            AccountLoginType.LDAP => q.Where(r => r.Sid != null),
            AccountLoginType.SSO => q.Where(r => r.SsoNameId != null),
            AccountLoginType.Standart => q.Where(r => r.SsoNameId == null && r.Sid == null),
            _ => q
        };

        return q;
    }

    private static async Task<List<Guid>> CollectGroupChildAsync(UserDbContext userDbContext, int tenant, Guid id)
    {
        var result = new List<Guid>();

        var children = Queries.GroupIdsAsync(userDbContext, tenant, id);

        await foreach (var child in children)
        {
            result.Add(child);
            result.AddRange(await CollectGroupChildAsync(userDbContext, tenant, child));
        }

        result.Add(id);

        return result.Distinct().ToList();
    }

    public async Task<UserInfo> GetUserAsync(int tenant, Guid id, Expression<Func<User, UserInfo>> exp)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        var q = GetUserQuery(userDbContext, tenant).Where(r => r.Id == id);

        if (exp != null)
        {
            return await q.Select(exp).FirstOrDefaultAsync();
        }

        return await q.ProjectTo<UserInfo>(mapper.ConfigurationProvider).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<string>> GetDavUserEmailsAsync(int tenant)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.EmailsAsync(userDbContext, tenant).ToListAsync();
    }

    private string GetPasswordHash(Guid userId, string password)
    {
        return Hasher.Base64Hash(password + userId + Encoding.UTF8.GetString(machinePseudoKeys.GetMachineConstant()), HashAlg.SHA512);
    }
    
    private static IQueryable<DbGroup> BuildTextSearch(string text, IQueryable<DbGroup> q)
    {
        if (string.IsNullOrEmpty(text))
        {
            return q;
        }

        text = text.ToLower().Trim();
            
        q = q.Where(g => g.Name.ToLower().Contains(text));

        return q;
    }

    private static IQueryable<DbGroup> BuildUserGroupSearch(Guid userId, bool manager, IQueryable<DbGroup> q, UserDbContext userDbContext)
    {
        var refType = manager ? UserGroupRefType.Manager : UserGroupRefType.Contains;

        var q1 = q.Join(userDbContext.UserGroups, group => group.Id, userGroup => userGroup.UserGroupId,
                (group, userGroup) => new { group, userGroup })
            .Where(r => !r.userGroup.Removed && r.userGroup.Userid == userId && r.userGroup.RefType == refType).Select(r => r.group);
        return q1;
    }
}

public class DbUserSecurity
{
    public User User { get; init; }
    public UserSecurity UserSecurity { get; init; }
}

public class UserWithGroup
{
    public User User { get; init; }
    public UserGroup Group { get; init; }
}

static file class Queries
{
    public static readonly Func<UserDbContext, int, Guid, Task<DateTime?>> LastModifiedAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.UserSecurity
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId)
                    .Select(r => r.LastModified)
                    .FirstOrDefault());

    public static readonly Func<UserDbContext, int, Guid, Task<byte[]>> PhotoAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Photos
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId)
                    .Select(r => r.Photo)
                    .FirstOrDefault());

    public static readonly Func<UserDbContext, DateTime, IAsyncEnumerable<int>> TenantIdsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, DateTime from) =>
                ctx.Users
                    .Where(u => u.LastModified > from)
                    .Select(u => u.TenantId)
                    .Distinct());

    public static readonly Func<UserDbContext, int, Guid, IAsyncEnumerable<Guid>> GroupIdsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid parentId) =>
                ctx.Groups
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentId == parentId)
                    .Select(r => r.Id));

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>> DeleteAclByIdsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Acl
                    .Where(r => r.TenantId == tenantId
                                && ids.Any(i => i == r.Subject))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<string>, Task<int>>
        DeleteSubscriptionsByIdsAsync = EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<string> ids) =>
                ctx.Subscriptions
                    .Where(r => r.TenantId == tenantId
                                && ids.Any(i => i == r.Recipient))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<string>, Task<int>>
        DeleteDbSubscriptionMethodsByIdsAsync = EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<string> ids) =>
                ctx.SubscriptionMethods
                    .Where(r => r.TenantId == tenantId
                                && ids.Any(i => i == r.Recipient))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>>
        DeleteUserGroupsByIdsAsync = EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId
                                && ids.Any(i => i == r.UserGroupId))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>>
        UpdateUserGroupsByIdsAsync = EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId
                                && ids.Any(i => i == r.UserGroupId))
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                         .SetProperty(p => p.LastModified, DateTime.UtcNow)));

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>> DeleteDbGroupsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Groups
                    .Where(r => r.TenantId == tenantId
                                && ids.Any(i => i == r.Id))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>> UpdateDbGroupsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Groups
                    .Where(r => r.TenantId == tenantId
                                && ids.Any(i => i == r.Id))
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                         .SetProperty(p => p.LastModified, DateTime.UtcNow)));

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteAclAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid id) =>
                ctx.Acl
                    .Where(r => r.TenantId == tenantId
                                && r.Subject == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, string, Task<int>> DeleteSubscriptionsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string id) =>
                ctx.Subscriptions
                    .Where(r => r.TenantId == tenantId
                                && r.Recipient == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, string, Task<int>>
        DeleteDbSubscriptionMethodsAsync = EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string id) =>
                ctx.SubscriptionMethods
                    .Where(r => r.TenantId == tenantId
                                && r.Recipient == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteUserPhotosAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Photos
                    .Where(r => r.TenantId == tenantId && r.UserId == userId)
                    .ExecuteDelete());
    
    public static readonly Func<UserDbContext, string, Task<int>> DeleteAccountLinksAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, string id) =>
                ctx.AccountLinks
                    .Where(r => r.Id == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteUserGroupsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId
                                && r.Userid == userId)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Task<int>> UpdateUserGroupsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId
                                && r.Userid == userId)
                    .ExecuteUpdate(q=> q.SetProperty(p => p.Removed, true)
                                        .SetProperty(p => p.LastModified, DateTime.UtcNow)));

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteUsersAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid id) =>
                ctx.Users.Where(r => r.TenantId == tenantId
                                     && r.Id == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Task<int>> UpdateUsersAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid id) =>
                ctx.Users.Where(r => r.TenantId == tenantId
                                     && r.Id == id)
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                         .SetProperty(p => p.LastModified, DateTime.UtcNow)
                                         .SetProperty(p => p.TerminatedDate, DateTime.UtcNow)
                                         .SetProperty(p => p.Status, EmployeeStatus.Terminated)));

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteUserSecuritiesAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.UserSecurity
                    .Where(r => r.TenantId == tenantId
                                && r.UserId == userId)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Guid, UserGroupRefType, Task<int>>
        DeleteUserGroupsByGroupIdAsync = EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId, Guid groupId, UserGroupRefType refType) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId
                                && r.Userid == userId
                                && r.UserGroupId == groupId
                                && r.RefType == refType)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Guid, UserGroupRefType, Task<int>>
        UpdateUserGroupsByGroupIdAsync = EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId, Guid groupId, UserGroupRefType refType) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId
                                && r.Userid == userId
                                && r.UserGroupId == groupId
                                && r.RefType == refType)
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                         .SetProperty(p => p.LastModified, DateTime.UtcNow)));

    public static readonly Func<UserDbContext, int, Guid, Task<User>> UserAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Users
                    .First(r => r.TenantId == tenantId
                                && r.Id == userId));

    public static readonly Func<UserDbContext, int, string, Guid, Task<bool>> AnyUsersAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string userName, Guid id) =>
                ctx.Users
                    .Where(r => tenantId == Tenant.DefaultTenant || r.TenantId == tenantId)
                    .Any(r => r.UserName == userName && r.Id != id && !r.Removed));

    public static readonly Func<UserDbContext, int, string, Guid, Task<bool>> AnyUsersByEmailAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string email, Guid id) =>
                ctx.Users
                    .Where(r => tenantId == Tenant.DefaultTenant || r.TenantId == tenantId)
                    .Any(r => r.Email == email && r.Id != id && !r.Removed));

    public static readonly Func<UserDbContext, int, Guid, Task<User>> FirstOrDefaultUserAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid id) =>
                ctx.Users
                    .Where(r => tenantId == Tenant.DefaultTenant || r.TenantId == tenantId)
                    .FirstOrDefault(a => a.Id == id));

    public static readonly Func<UserDbContext, int, Guid, Task<UserPhoto>> UserPhotoAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Photos
                    .FirstOrDefault(r => r.UserId == userId
                                         && r.TenantId == tenantId));

    public static readonly Func<UserDbContext, int, IAsyncEnumerable<string>> EmailsAsync =
        EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId) =>
                (from usersDav in ctx.UsersDav
                 join users in ctx.Users on new { tenant = usersDav.TenantId, userId = usersDav.UserId } equals new
                 {
                     tenant = users.TenantId,
                     userId = users.Id
                 }
                 where usersDav.TenantId == tenantId
                 select users.Email)
                .Distinct());
}