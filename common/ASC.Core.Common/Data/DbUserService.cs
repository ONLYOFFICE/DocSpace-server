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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Core.Data;

[Scope]
public class EFUserService(
    IDbContextFactory<UserDbContext> dbContextFactory,
    MachinePseudoKeys machinePseudoKeys,
    IMapper mapper,
    AuthContext authContext)
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

        var q = BuildBaseGroupQuery(tenant, userDbContext);
        q = BuildTextSearch(text, q);

        if (userId != Guid.Empty)
        {
            q = BuildUserGroupSearch(userId, manager, q, userDbContext);
        }

        switch (sortBy)
        {
            case GroupSortType.Manager:
                {
                    var q1 = q.Select(g => new
                    {
                        Group = g,
                        Manager = userDbContext.UserGroups
                            .Where(ug => ug.TenantId == tenant && !ug.Removed && ug.UserGroupId == g.Id && ug.RefType == UserGroupRefType.Manager)
                            .Join(userDbContext.Users, ug => ug.Userid, u => u.Id, (ug, u) => u)
                            .FirstOrDefault()
                    });

                    q = (sortOrderAsc ?
                        q1.OrderBy(r => r.Manager.FirstName) :
                        q1.OrderByDescending(r => r.Manager.FirstName)).Select(r => r.Group);
                    break;
                }
            case GroupSortType.MembersCount:
                {
                    var q1 = q.Select(g => new
                    {
                        Group = g,
                        MembersCount = userDbContext.UserGroups
                            .Count(ug => ug.TenantId == tenant && ug.UserGroupId == g.Id && ug.RefType == UserGroupRefType.Contains && !ug.Removed)
                    });

                    q = (sortOrderAsc ?
                        q1.OrderBy(r => r.MembersCount) :
                        q1.OrderByDescending(r => r.MembersCount)).Select(r => r.Group);
                    break;
                }
            default:
                q = sortOrderAsc ? q.OrderBy(g => g.Name) : q.OrderByDescending(g => g.Name);
                break;
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

        var q = BuildBaseGroupQuery(tenant, userDbContext);

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
        var user = await userDbContext.UserByIdAsync(tenant, id);
        return mapper.Map<UserInfo>(user);
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
        return mapper.Map<UserInfo>(await userDbContext.UserByEmailAsync(tenant, email));
    }

    public async Task<UserInfo> GetUserByUserName(int tenant, string userName)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return mapper.Map<UserInfo>(await userDbContext.UserByUserNameAsync(tenant, userName));
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

    public async Task<UserGroupRef> GetUserGroupRefAsync(int tenant, Guid groupId, UserGroupRefType refType)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

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
        return (mapper.Map<List<UserGroup>, IEnumerable<UserGroupRef>>(await userDbContext.GroupsByTenantAsync(tenant).ToListAsync())).ToDictionary(r => r.CreateKey(), r => r);
    }

    public async Task<DateTime> GetUserPasswordStampAsync(int tenant, Guid id)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var stamp = await userDbContext.LastModifiedAsync(tenant, id);

        return stamp ?? DateTime.MinValue;
    }

    public async Task<byte[]> GetUserPhotoAsync(int tenant, Guid id)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var photo = await userDbContext.PhotoAsync(tenant, id);

        return photo ?? [];
    }

    public async Task<IEnumerable<UserInfo>> GetUsersAsync(int tenant)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return mapper.Map<List<User>, IEnumerable<UserInfo>>(await userDbContext.UserByTenantAsync(tenant).ToListAsync());
    }

    public async Task<int> GetUsersCountAsync(UserQueryFilter filter)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = GetUserQuery(userDbContext, filter.TenantId);

        q = GetUserQueryForFilter(userDbContext, q, filter);

        return await q.CountAsync();
    }

    public async IAsyncEnumerable<UserInfo> GetUsers(UserQueryFilter filter)
    {
        if (filter.Limit <= 0)
        {
            yield break;
        }

        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = GetUserQuery(userDbContext, filter.TenantId);

        q = GetUserQueryForFilter(userDbContext, q, filter);

        switch (filter.SortType)
        {
            case UserSortType.CreatedBy:
                {
                    var q1 = q.GroupJoin(userDbContext.Users,
                        u1 => new
                        {
                            Id = u1.CreatedBy, 
                            Removed = false
                        },
                        u2 => new
                        {
                            Id = (Guid?)u2.Id, 
                            u2.Removed
                        }, 
                        (u, users) => new { u, users })
                        .SelectMany(
                            x => x.users.DefaultIfEmpty(), 
                            (x, createdBy) => new { x.u, createdBy });

                    if (UserFormatter.GetUserDisplayDefaultOrder() == DisplayUserNameFormat.FirstLast)
                    {
                        q = (filter.SortOrderAsc
                                ? q1.OrderBy(x => x.createdBy.FirstName)
                                : q1.OrderByDescending(x => x.createdBy.FirstName))
                            .Select(x => x.u);
                    }
                    else
                    {
                        q = (filter.SortOrderAsc
                                ? q1.OrderBy(x => x.createdBy.LastName)
                                : q1.OrderByDescending(x => x.createdBy.LastName))
                            .Select(x => x.u);
                    }
                    break;
                }
            case UserSortType.Type:
                {
                    var q1 = (from user in q
                              join userGroup in userDbContext.UserGroups.Where(g =>
                                  !g.Removed && (g.UserGroupId == Constants.GroupAdmin.ID || g.UserGroupId == Constants.GroupGuest.ID ||
                                                 g.UserGroupId == Constants.GroupUser.ID)) on user.Id equals userGroup.Userid into joinedGroup
                              from @group in joinedGroup.DefaultIfEmpty()
                              select new UserWithGroup { User = user, Group = @group });

                    Expression<Func<UserWithGroup, int>> orderByUserType = u =>
                        u.User.Id == filter.OwnerId ? 0 :
                        u.Group == null ? 2 :
                        u.Group.UserGroupId == Constants.GroupAdmin.ID ? 1 :
                        u.Group.UserGroupId == Constants.GroupUser.ID ? 3 : 4;

                    q = (filter.SortOrderAsc ? q1.OrderBy(orderByUserType).ThenBy(x => x.User.FirstName) 
                        : q1.OrderByDescending(orderByUserType)).ThenBy(x => x.User.FirstName).Select(r => r.User);
                    break;
                }
            case UserSortType.UsedSpace:
                {
                    var q2 = from user in q
                             join quota in userDbContext.QuotaRow.Where(qr => qr.UserId != Guid.Empty && qr.Tag != Guid.Empty.ToString() && qr.Tag != "")
                                on user.Id equals quota.UserId into quotaRow
                             from @quota in quotaRow.DefaultIfEmpty()

                             select new { user, @quota };

                    var q3 = q2.GroupBy(q => q.user, q => q.quota.Counter, (user, g) => new
                    {
                        user,
                        sum_counter = g.ToList().Sum()
                    });

                    q = filter.SortOrderAsc 
                        ? q3.OrderBy(r => r.sum_counter).Select(r => r.user) 
                        : q3.OrderByDescending(r => r.sum_counter).Select(r => r.user);
                    break;
                }
            case UserSortType.Department:
                {
                    var q1 = q.Select(u => new
                    {
                        user = u,
                        groupsCount = userDbContext.UserGroups.Count(g =>
                            g.TenantId == filter.TenantId && g.Userid == u.Id && !g.Removed && g.RefType == UserGroupRefType.Contains &&
                            !Constants.SystemGroups.Select(sg => sg.ID).Contains(g.UserGroupId))
                    });

                    q = (filter.SortOrderAsc
                            ? q1.OrderBy(r => r.groupsCount).ThenBy(r => r.user.FirstName)
                            : q1.OrderByDescending(r => r.groupsCount)).ThenByDescending(r => r.user.FirstName)
                        .Select(r => r.user);
                    break;
                }
            case UserSortType.Email:
                q = (filter.SortOrderAsc ? q.OrderBy(u => u.Email) : q.OrderByDescending(u => u.Email));
                break;
            case UserSortType.LastName:
                q = filter.SortOrderAsc
                    ? q.OrderBy(r => r.Status == EmployeeStatus.Active ? 0 : r.Status == EmployeeStatus.Pending ? 1 : 2)
                       .ThenBy(u => u.Status == EmployeeStatus.Pending ? u.Email : u.LastName)
                    : q.OrderBy(r => r.Status == EmployeeStatus.Active ? 0 : r.Status == EmployeeStatus.Pending ? 1 : 2)
                       .ThenByDescending(u => u.Status == EmployeeStatus.Pending ? u.Email : u.LastName);
                break;
            case UserSortType.FirstName:
            default:
                q = filter.SortOrderAsc
                    ? q.OrderBy(r => r.Status ==  EmployeeStatus.Active ? 0 : r.Status == EmployeeStatus.Pending ? 1 : 2).ThenBy(u => u.Status ==  EmployeeStatus.Pending ? u.Email : u.FirstName)
                    : q.OrderBy(r => r.Status ==  EmployeeStatus.Active ? 0 : r.Status == EmployeeStatus.Pending ? 1 : 2).ThenByDescending(u => u.Status ==  EmployeeStatus.Pending ? u.Email : u.FirstName);
                break;
        }

        if (filter.Offset > 0)
        {
            q = q.Skip((int)filter.Offset);
        }

        q = q.Take((int)filter.Limit);

        await foreach (var user in q.ToAsyncEnumerable())
        {
            yield return mapper.Map<User, UserInfo>(user);
        }
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

            await userDbContext.DeleteAclByIdsAsync(tenant, ids);
            await userDbContext.DeleteSubscriptionsByIdsAsync(tenant, stringIds);
            await userDbContext.DeleteDbSubscriptionMethodsByIdsAsync(tenant, stringIds);

            if (immediate)
            {
                await userDbContext.DeleteUserGroupsByIdsAsync(tenant, ids);
                await userDbContext.DeleteDbGroupsAsync(tenant, ids);
            }
            else
            {
                await userDbContext.UpdateUserGroupsByIdsAsync(tenant, ids);
                await userDbContext.UpdateDbGroupsAsync(tenant, ids);
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

            await userDbContext.DeleteAclAsync(tenant, id);
            await userDbContext.DeleteSubscriptionsAsync(tenant, id.ToString());
            await userDbContext.DeleteDbSubscriptionMethodsAsync(tenant, id.ToString());
            await userDbContext.DeleteUserPhotosAsync(tenant, id);
            await userDbContext.DeleteAccountLinksAsync(id.ToString());

            if (immediate)
            {
                await userDbContext.DeleteUserGroupsAsync(tenant, id);
                await userDbContext.DeleteUsersAsync(tenant, id);
                await userDbContext.DeleteUserSecuritiesAsync(tenant, id);
            }
            else
            {
                await userDbContext.UpdateUserGroupsAsync(tenant, id);
                await userDbContext.UpdateUsersAsync(tenant, id);
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
                await dbContext.DeleteUserGroupsByGroupIdAsync(tenant, userId, groupId, refType);
            }
            else
            {
                await dbContext.UpdateUserGroupsByGroupIdAsync(tenant, userId, groupId, refType);
            }

            var user = await dbContext.UserAsync(tenant, userId);
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

        var any = await userDbContext.AnyUsersAsync(tenant, user.UserName, user.Id);

        if (any)
        {
            throw new ArgumentException($"Duplicate {nameof(user.UserName)}");
        }

        any = await userDbContext.AnyUsersByEmailAsync(tenant, user.Email, user.Id);

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

        var user = await userDbContext.FirstOrDefaultUserAsync(tenant, userGroupRef.UserId);
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

        var userPhoto = await userDbContext.UserPhotoAsync(tenant, id);
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

    public async Task SaveUsersRelationAsync(int tenantId, Guid sourceUserId, Guid targetUserId)
    {
        var relation = new DbUserRelation
        {
            TenantId = tenantId,
            SourceUserId = sourceUserId,
            TargetUserId = targetUserId
        };

        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        var existedRecord = await userDbContext.UserRelations.FindAsync(relation.GetKeys());
        if (existedRecord != null)
        {
            return;
        }
        
        await userDbContext.UserRelations.AddAsync(relation);
        await userDbContext.SaveChangesAsync();
    }

    public async Task<Dictionary<Guid, UserRelation>> GetUserRelationsAsync(int tenantId, Guid sourceUserId)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        
        var q = userDbContext.UserRelations
            .Where(r => r.TenantId == tenantId && r.SourceUserId == sourceUserId);
        
        return await q.ToDictionaryAsync(r => r.TargetUserId, mapper.Map<UserRelation>);
    }

    private IQueryable<User> GetUserQuery(UserDbContext userDbContext, int tenant)
    {
        var q = userDbContext.Users.AsQueryable();

        if (tenant != Tenant.DefaultTenant)
        {
            q = q.Where(r => r.TenantId == tenant);
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

    private IQueryable<User> GetUserQueryForFilter(UserDbContext userDbContext, IQueryable<User> q, UserQueryFilter filter)
    {
        q = q.Where(r => !r.Removed);

        switch (filter.Area)
        {
            case Area.Guests:
                filter.IncludeGroups.Add([Constants.GroupGuest.ID]);
                break;
            case Area.People:
                filter.ExcludeGroups.Add(Constants.GroupGuest.ID);
                break;
        }

        if (filter.IncludeGroups is { Count: > 0 } || filter.ExcludeGroups is { Count: > 0 })
        {
            if (filter.IncludeGroups is { Count: > 0 })
            {
                foreach (var ig in filter.IncludeGroups)
                {
                    q = q.Where(r => userDbContext.UserGroups.Any(a => !a.Removed && a.TenantId == r.TenantId && a.Userid == r.Id && ig.Any(id => id == a.UserGroupId)));
                }
            }

            if (filter.ExcludeGroups is { Count: > 0 })
            {
                foreach (var eg in filter.ExcludeGroups)
                {
                    q = q.Where(r => !userDbContext.UserGroups.Any(a => !a.Removed && a.TenantId == r.TenantId && a.Userid == r.Id && a.UserGroupId == eg));
                }
            }
        }
        else if (filter.CombinedGroups != null && filter.CombinedGroups.Count != 0)
        {
            Expression<Func<User, bool>> a = r => false;

            foreach (var (cgIncludeGroups, cgExcludeGroups) in filter.CombinedGroups)
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

        if (filter.QuotaFilter != null && filter.IsDocSpaceAdmin)
        {
            if (filter.QuotaFilter == QuotaFilter.Custom)
            {
                q = q.Where(r => userDbContext.WebstudioSettings.Any(a => a.TenantId == r.TenantId && a.Id == new UserQuotaSettings().ID && a.UserId == r.Id));
            }
            else if (filter.QuotaFilter == QuotaFilter.Default)
            {
                q = q.Where(r => !userDbContext.WebstudioSettings.Any(a => a.TenantId == r.TenantId && a.Id == new UserQuotaSettings().ID && a.UserId == r.Id));
            }
        }

        if (filter.WithoutGroup)
        {
            q = from user in q
                join userGroup in userDbContext.UserGroups.Where(g =>
                        !g.Removed && !Constants.SystemGroups.Select(gi => gi.ID).Contains(g.UserGroupId))
                    on user.Id equals userGroup.Userid into joinedSet
                from @group in joinedSet.DefaultIfEmpty()
                where @group == null
                select user;
        }

        if (!filter.IsDocSpaceAdmin && filter.EmployeeStatus == null)
        {
            q = q.Where(r => r.Status != EmployeeStatus.Terminated);
        }

        if (filter.EmployeeStatus != null)
        {
            switch (filter.EmployeeStatus)
            {
                case EmployeeStatus.Pending:
                    q = q.Where(u => u.Status == EmployeeStatus.Pending);
                    break;
                case EmployeeStatus.Terminated:
                    q = filter.IsDocSpaceAdmin ? q.Where(u => u.Status == EmployeeStatus.Terminated) : q.Where(u => false);
                    break;
                case EmployeeStatus.All:
                    if (!filter.IsDocSpaceAdmin)
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

        if (filter.ActivationStatus != null)
        {
            q = q.Where(r => r.ActivationStatus == filter.ActivationStatus.Value);
        }

        switch (filter.Area)
        {
            case Area.Guests when !filter.IncludeStrangers:
                {
                    var currentUserId = authContext.CurrentAccount.ID;
            
                    q = q.Join(userDbContext.UserRelations,
                        u => new
                        {
                            filter.TenantId,
                            SourceUserId = currentUserId, 
                            TargetUserId = u.Id
                        },
                        ur => new
                        {
                            ur.TenantId,
                            ur.SourceUserId, 
                            ur.TargetUserId
                        },
                        (u, ur) => u);
                    break;
                }
            case Area.All when !filter.IncludeStrangers:
                {
                    var currentUserId = authContext.CurrentAccount.ID;
            
                    q = q.GroupJoin(userDbContext.UserRelations,
                            u => new
                            {
                                filter.TenantId,
                                SourceUserId = currentUserId, 
                                TargetUserId = u.Id
                            },
                            ur => new
                            {
                                ur.TenantId,
                                ur.SourceUserId, 
                                ur.TargetUserId
                            },
                            (u, ur) => new { u, ur })
                        .SelectMany(
                            x => x.ur.DefaultIfEmpty(),
                            (x, ur) => new { x.u, ur })
                        .GroupJoin(userDbContext.UserGroups,
                            x => new
                            {
                                filter.TenantId,
                                Userid = x.u.Id,
                                UserGroupId = Constants.GroupGuest.ID,
                                RefType = UserGroupRefType.Contains,
                                Removed = false
                            },
                            ug => new
                            {
                                ug.TenantId,
                                ug.Userid,
                                ug.UserGroupId,
                                ug.RefType,
                                ug.Removed
                            },
                            (x, ug) => new { x.u, x.ur, ug })
                        .SelectMany(
                            x => x.ug.DefaultIfEmpty(),
                            (x, ug) => new { x.u, x.ur, ug })
                        .Where(x => 
                            x.ug == null ||
                            (x.ur != null && x.ug != null))
                        .Select(x => x.u);
                    break;
                }
        }

        q = UserQueryHelper.FilterByText(q, filter.Text, filter.Separator);

        q = filter.AccountLoginType switch
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

        var children = userDbContext.GroupIdsAsync(tenant, id);

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
        return await userDbContext.EmailsAsync(tenant).ToListAsync();
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

    private static IQueryable<DbGroup> BuildBaseGroupQuery(int tenant, UserDbContext userDbContext)
    {
        return userDbContext.Groups.Where(g => g.TenantId == tenant && !g.Removed);
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