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

using User = ASC.Core.Common.EF.User;

namespace ASC.Files.Core.Data;

[Scope]
internal abstract class SecurityBaseDao<T>(
    IDaoFactory daoFactory,
    UserManager userManager,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    SetupInfo setupInfo,
    MaxTotalSizeStatistic maxTotalSizeStatistic,
    SettingsManager settingsManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IMapper mapper,
    IDistributedLockProvider distributedLockProvider)
    : AbstractDao(dbContextFactory,
        userManager,
        tenantManager,
        tenantUtil,
        setupInfo,
        maxTotalSizeStatistic,
        settingsManager,
        authContext,
        serviceProvider,
        distributedLockProvider)
{
    public async Task DeleteShareRecordsAsync(IEnumerable<FileShareRecord<T>> records)
    {
        var mapping = daoFactory.GetMapping<T>();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (var record in records)
        {
            var query = filesDbContext.ForDeleteShareRecordsAsync(record.TenantId, record.EntryType, record.Subject)
            .WhereAwait(async r => r.EntryId == (await mapping.MappingIdAsync(record.EntryId)));

            filesDbContext.RemoveRange(query);
        }
        await filesDbContext.SaveChangesAsync();
    }

    public async Task<bool> IsPureSharedAsync(T entryId, FileEntryType type, IEnumerable<SubjectType> subjectTypes)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        var mappedId = await daoFactory.GetMapping<T>().MappingIdAsync(entryId);
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.IsPureSharedAsync(tenantId, mappedId, type, subjectTypes);
    }

    public async Task SetShareAsync(FileShareRecord<T> r)
    {
        var mapping = daoFactory.GetMapping<T>();
        if (r.Share == FileShare.None)
        {
            var entryId = await mapping.MappingIdAsync(r.EntryId);
            if (string.IsNullOrEmpty(entryId))
            {
                return;
            }

            var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
            
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync();
                await using var tr = await context.Database.BeginTransactionAsync();

                var files = new List<string>();

                if (r.EntryType == FileEntryType.Folder)
                {
                    var folders = new List<string>();
                    if (int.TryParse(entryId, out _))
                    {
                        var foldersInt = await context.FolderIdsAsync(entryId).ToListAsync();

                        folders.AddRange(foldersInt.Select(folderInt => folderInt.ToString()));
                        files.AddRange(await context.FilesIdsAsync(tenantId, foldersInt).ToListAsync());
                    }
                    else
                    {
                        folders.Add(entryId);
                    }
                    await context.DeleteForSetShareAsync(r.TenantId, r.Subject, folders, FileEntryType.Folder);
                }
                else
                {
                    files.Add(entryId);
                }

                if (files.Count > 0)
                {
                    await context.DeleteForSetShareAsync(r.TenantId, r.Subject, files, FileEntryType.File);
                }

                await context.SaveChangesAsync();
                await tr.CommitAsync();
            });
        }
        else
        {
            var toInsert = mapper.Map<FileShareRecord<T>, DbFilesSecurity>(r);
            toInsert.EntryId = await mapping.MappingIdAsync(r.EntryId, true);

            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            await filesDbContext.AddOrUpdateAsync(context => context.Security, toInsert);
            await filesDbContext.SaveChangesAsync();
        }
    }

    public async IAsyncEnumerable<FileShareRecord<T>> GetSharesAsync(IEnumerable<Guid> subjects)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = filesDbContext.SharesAsync(tenantId, subjects);

        await foreach (var e in q)
        {
            yield return await ToFileShareRecordAsync(e);
        }
    }

    public IAsyncEnumerable<FileShareRecord<T>> GetPureShareRecordsAsync(IEnumerable<FileEntry<T>> entries)
    {
        if (entries == null)
        {
            return AsyncEnumerable.Empty<FileShareRecord<T>>();
        }

        return InternalGetPureShareRecordsAsync(entries);
    }

    internal async IAsyncEnumerable<FileShareRecord<T>> InternalGetPureShareRecordsAsync(IEnumerable<FileEntry<T>> entries)
    {
        var files = new List<string>();
        var folders = new List<string>();

        foreach (var entry in entries)
        {
            await SelectFilesAndFoldersForShareAsync(entry, files, folders, null);
        }

        await foreach (var e in GetPureShareRecordsDbAsync(files, folders))
        {
            yield return e;
        }
    }

    public IAsyncEnumerable<FileShareRecord<T>> GetPureShareRecordsAsync(FileEntry<T> entry)
    {
        if (entry == null)
        {
            return AsyncEnumerable.Empty<FileShareRecord<T>>();
        }

        return InternalGetPureShareRecordsAsync(entry);
    }

    public async IAsyncEnumerable<GroupMemberSecurityRecord> GetGroupMembersWithSecurityAsync(FileEntry<T> entry, Guid groupId, string text, int offset, int count)
    {
        if (entry == null)
        {
            yield break;
        }

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var mappedId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var groupShare = await filesDbContext.Security
            .Where(s => s.TenantId == tenantId && s.EntryId == mappedId && s.EntryType == entry.FileEntryType && s.Subject == groupId)
            .Select(s => s.Share)
            .FirstOrDefaultAsync();

        if (groupShare == FileShare.None)
        {
            yield break;
        }

        var users = filesDbContext.Users
            .Join(filesDbContext.UserGroup, user => user.Id, ug => ug.Userid, (user, ug) => new { user, ug })
            .Where(r => r.ug.TenantId == tenantId && r.ug.UserGroupId == groupId && !r.ug.Removed && r.ug.RefType == UserGroupRefType.Contains)
            .Select(r => r.user);
        
        if (!string.IsNullOrEmpty(text))
        {
            users = users.Where(u => u.FirstName.ToLower().Contains(text) || u.LastName.ToLower().Contains(text) || u.Email.ToLower().Contains(text));
        }

        var q = users
            .GroupJoin(filesDbContext.Security, user => user.Id, dbFilesSecurity => dbFilesSecurity.Subject,
                (user, securities) => new { user, securities })
            .OrderBy(r => r.user.FirstName)
            .SelectMany(r => r.securities
                .Where(s => s.TenantId == tenantId && s.EntryId == mappedId && s.EntryType == entry.FileEntryType)
                .DefaultIfEmpty(), (u, s) => new GroupMemberSecurityRecord
            {
                UserId = u.user.Id,
                UserShare = s.Share,
                GroupShare = groupShare
            });

        if (offset > 0)
        {
            q = q.Skip(offset);
        }

        if (count > 0)
        {
            q = q.Take(count);
        }

        await foreach (var record in q.ToAsyncEnumerable())
        {
            yield return record;
        }
    }

    public async Task<int> GetGroupMembersWithSecurityCountAsync(FileEntry<T> entry, Guid groupId, string text)
    {
        if (entry == null)
        {
            return 0;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q = filesDbContext.UserGroup.Where(g =>
            g.TenantId == tenantId && g.UserGroupId == groupId && !g.Removed && g.RefType == UserGroupRefType.Contains);

        if (string.IsNullOrEmpty(text))
        {
            return await q.CountAsync();
        }

        text = GetSearchText(text);
            
        return await q.Join(filesDbContext.Users, ug => ug.Userid, u => u.Id, (ug, u) => u)
            .Where(u => u.FirstName.ToLower().Contains(text) || u.LastName.ToLower().Contains(text) || u.Email.ToLower().Contains(text)).CountAsync();
    }

    public async Task<int> GetPureSharesCountAsync(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text)
    {
        if (entry == null)
        {
            return 0;
        }

        text = GetSearchText(text);

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        var q = await GetPureSharesQuery(tenantId, entry, filterType, filesDbContext);
        var textSearch = !string.IsNullOrEmpty(text);

        if (filterType is not (ShareFilterType.User or ShareFilterType.Group or ShareFilterType.UserOrGroup))
        {
            return await q.CountAsync();
        }

        switch (filterType)
        {
            case ShareFilterType.UserOrGroup:
                {
                    var userQuery = q.Join(filesDbContext.Users, s => s.Subject, u => u.Id,
                            (security, user) => new { security, user })
                        .Where(r => !r.user.Removed);

                    var groupQuery = q.Where(s => s.SubjectType == SubjectType.Group);

                    if (textSearch)
                    {
                        userQuery = userQuery.Where(r => 
                            r.user.FirstName.ToLower().Contains(text) || r.user.LastName.ToLower().Contains(text) || r.user.Email.ToLower().Contains(text));
                        
                        groupQuery = groupQuery.Join(filesDbContext.Groups, s => s.Subject, g => g.Id, (s, g) => new { s, g })
                            .Where(r => r.g.Name.ToLower().Contains(text))
                            .Select(r => r.s);
                    }

                    q = userQuery.Select(r => r.security).Concat(groupQuery);
                    break;
                }
            case ShareFilterType.User:
                {
                    var q1 = q.Join(filesDbContext.Users, s => s.Subject, u => u.Id,
                        (s, u) => new SecurityUserRecord { Security = s, User = u }).Where(r => !r.User.Removed);

                    if (textSearch)
                    {
                        q1 = q1.Where(r => r.User.FirstName.ToLower().Contains(text) || r.User.LastName.ToLower().Contains(text) || r.User.Email.ToLower().Contains(text));
                    }

                    if (status.HasValue)
                    {
                        q1 = q1.Where(s => s.User.ActivationStatus == status.Value);
                    }

                    q = q1.Select(r => r.Security);
                    break;
                }
            case ShareFilterType.Group when textSearch:
                q = q.Join(filesDbContext.Groups, s => s.Subject, g => g.Id, (security, group) => new { security, group })
                    .Where(r => r.group.Name.ToLower().Contains(text))
                    .Select(r => r.security);
                break;
        }

        return await q.CountAsync();
    }

    public async IAsyncEnumerable<FileShareRecord<T>> GetPureSharesAsync(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text, int offset = 0, int count = -1)
    {
        if (entry == null || count == 0)
        {
            yield break;
        }

        text = GetSearchText(text);

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var searchByText = !string.IsNullOrEmpty(text);
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        var q = await GetPureSharesQuery(tenantId, entry, filterType, filesDbContext);

        switch (filterType)
        {
            case ShareFilterType.User when (entry is IFolder folder && DocSpaceHelper.IsRoom(folder.FolderType)):
                {
                    var predicate = ShareCompareHelper.GetCompareExpression<SecurityUserRecord>(s => s.Security.Share, entry.RootFolderType);

                    var q1 = q.Join(filesDbContext.Users, s => s.Subject, u => u.Id,
                        (security, user) => new SecurityUserRecord { Security = security, User = user })
                        .Where(r => !r.User.Removed);

                    if (searchByText)
                    {
                        q1 = q1.Where(r => r.User.FirstName.ToLower().Contains(text) || r.User.LastName.ToLower().Contains(text) || r.User.Email.ToLower().Contains(text));
                    }

                    if (status.HasValue)
                    {
                        q1 = q1.Where(r => r.User.ActivationStatus == status.Value);
                    }

                    q = q1.OrderBy(r => r.User.ActivationStatus)
                        .ThenBy(predicate)
                        .ThenBy(r => r.User.FirstName)
                        .Select(r => r.Security);
                    break;
                }
            case ShareFilterType.Group:
                {
                    var q1 = q.Join(filesDbContext.Groups, s => s.Subject, g => g.Id,
                        (security, group) => new SecurityOrderRecord
                        {
                            Security = security,
                            Order = 0,
                            Name = group.Name
                        });
                    
                    if (searchByText)
                    {
                        q1 = q1.Where(r => r.Name.ToLower().Contains(text));
                    }

                    var predicate = ShareCompareHelper.GetCompareExpression<SecurityOrderRecord>(s => s.Security.Share, entry.RootFolderType);
                    
                    q = q1.OrderBy(predicate)
                        .ThenBy(r => r.Name)
                        .Select(r => r.Security);
                    break;
                }
            case ShareFilterType.UserOrGroup:
                {
                    var predicate = ShareCompareHelper.GetCompareExpression<SecurityOrderRecord>(r => r.Security.Share, entry.RootFolderType);

                    var userQuery = q.Join(filesDbContext.Users, s => s.Subject, u => u.Id,
                        (security, user) => new { security, user });

                    var groupQuery = q.Join(filesDbContext.Groups, s => s.Subject, g => g.Id,
                        (security, group) => new { security, group });

                    if (searchByText)
                    {
                        userQuery = userQuery.Where(r =>
                            !r.user.Removed && (r.user.FirstName.ToLower().Contains(text) || r.user.LastName.ToLower().Contains(text) || r.user.Email.ToLower().Contains(text)));
                        groupQuery = groupQuery.Where(r => r.group.Name.ToLower().Contains(text));
                    }

                    var userQuery1 = userQuery.GroupJoin(filesDbContext.UserGroup,
                            x => new
                            {
                                TenantId = tenantId,
                                Userid = x.user.Id,
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
                            (x, ug) => new { x.security, x.user, ug })
                        .SelectMany(
                            x => x.ug.DefaultIfEmpty(),
                            (x, ug) => new { x.security, x.user, ug });
                    
                    var userQuery2 = userQuery1.Select(r => new SecurityOrderRecord
                            {
                                Security = r.security, 
                        Order = r.user.ActivationStatus == EmployeeActivationStatus.Pending 
                            ? 4 
                            : r.ug != null 
                                ? 3 
                                : r.security.Share == FileShare.RoomManager 
                                    ? 0 
                                    : 2,
                        Name = r.user.ActivationStatus == EmployeeActivationStatus.Pending ? r.user.Email : r.user.FirstName
                            });

                    var groupQuery1 = groupQuery.Select(r => new SecurityOrderRecord
                    {
                                    Security = r.security,
                        Order = 1,
                        Name = r.group.Name
                                });

                    q = userQuery2.Concat(groupQuery1)
                        .OrderBy(r => r.Order)
                        .ThenBy(predicate)
                        .ThenBy(r => r.Name)
                        .Select(r => r.Security);

                    break;
                }
            case ShareFilterType.ExternalLink:
                {
                    var predicate = ShareCompareHelper.GetCompareExpression<DbFilesSecurity>(s => s.Share, entry.RootFolderType);
                    q = q.OrderBy(predicate).ThenByDescending(s => s.SubjectType);
                    break;
                }
            default:
                {
                    var predicate = ShareCompareHelper.GetCompareExpression<DbFilesSecurity>(s => s.Share, entry.RootFolderType);
                    q = q.OrderBy(predicate);
                    break;
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

        var records = q.ToAsyncEnumerable().SelectAwait(async r => await ToFileShareRecordAsync(r));

        await foreach (var r in DeleteExpiredAsync(records, filesDbContext))
        {
            yield return r;
        }
    }

    public async IAsyncEnumerable<GroupInfoWithShared> GetGroupsWithSharedAsync(FileEntry<T> entry, string text, bool excludeShared, int offset, int count)
    {
        if (entry == null || count == 0)
        {
            yield break;
        }

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var mappedId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = GetGroupsWithSharedQuery(tenantId, mappedId, text, entry, excludeShared, filesDbContext);

        if (offset > 0)
        {
            q = q.Skip(offset);
        }

        if (count > 0)
        {
            q = q.Take(count);
        }

        await foreach (var r in q.ToAsyncEnumerable())
        {
            yield return new GroupInfoWithShared
            { 
                GroupInfo = mapper.Map<DbGroup, GroupInfo>(r.Group),
                Shared = r.Shared 
            };
        }
    }

    public async Task<int> GetGroupsWithSharedCountAsync(FileEntry<T> entry, string text, bool excludeShared)
    {
        if (entry == null)
        {
            return 0;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var mappedId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);

        var q = GetGroupsWithSharedQuery(tenantId, mappedId, text, entry, excludeShared, filesDbContext);

        return await q.CountAsync();
    }

    private static IQueryable<GroupWithShared> GetGroupsWithSharedQuery(int tenantId, string entryId, string text, FileEntry entry, bool excludeShared, FilesDbContext filesDbContext)
    {
        var q = filesDbContext.Groups.Where(g => g.TenantId == tenantId && !g.Removed);

        if (!string.IsNullOrEmpty(text))
        {
            text = GetSearchText(text);
            
            q = q.Where(g => g.Name.ToLower().Contains(text));
        }

        var q1 = excludeShared
            ? q.Where(g => !filesDbContext.Security.Any(s => s.TenantId == tenantId && s.EntryType == entry.FileEntryType && s.EntryId == entryId && s.Subject == g.Id))
                .OrderBy(g => g.Name)
                .Select(g => new GroupWithShared {Group = g, Shared = false})
            : from @group in q
            join security in filesDbContext.Security.Where(s => s.TenantId == tenantId && s.EntryId == entryId && s.EntryType == entry.FileEntryType)
                on @group.Id equals security.Subject into joinedSet
            from s in joinedSet.DefaultIfEmpty()
            orderby @group.Name
            select new GroupWithShared { Group = @group, Shared = s != null };

        return q1;
    }

    public async IAsyncEnumerable<UserInfoWithShared> GetUsersWithSharedAsync(FileEntry<T> entry, string text, EmployeeStatus? employeeStatus, EmployeeActivationStatus? activationStatus, 
        bool excludeShared, string separator, bool excludeStrangers, Area area = Area.All, int offset = 0, int count = -1)
    {
        if (entry == null || count == 0)
        {
            yield break;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var mappedId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q1 = GetUsersWithSharedQuery(tenantId, mappedId, entry, text, employeeStatus, activationStatus, excludeShared, filesDbContext, separator, excludeStrangers, area);

        if (offset > 0)
        {
            q1 = q1.Skip(offset);
        }
        
        if (count > 0)
        {
            q1 = q1.Take(count);
        }

        await foreach (var r in q1.ToAsyncEnumerable())
        {
            yield return new UserInfoWithShared { UserInfo = mapper.Map<User, UserInfo>(r.User), Shared = r.Shared };
        }
    }

    public async Task<int> GetUsersWithSharedCountAsync(FileEntry<T> entry, string text, EmployeeStatus? employeeStatus, EmployeeActivationStatus? activationStatus,
        bool excludeShared, string separator, bool excludeStrangers, Area area)
    {
        if (entry == null)
        {
            return 0;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var mappedId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);
        
        var q1 = GetUsersWithSharedQuery(tenantId, mappedId, entry, text, employeeStatus, activationStatus, excludeShared, filesDbContext, separator, excludeStrangers, area);

        return await q1.CountAsync();
    }

    private IQueryable<UserWithShared> GetUsersWithSharedQuery(int tenantId, string entryId, FileEntry entry, string text, EmployeeStatus? employeeStatus, 
        EmployeeActivationStatus? activationStatus, bool excludeShared, FilesDbContext filesDbContext, string separator, bool includeStrangers, Area area)
    {
        var q = filesDbContext.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && !u.Removed);

        if (employeeStatus.HasValue)
        {
            q = q.Where(u => u.Status == employeeStatus.Value);
        }

        if (activationStatus.HasValue)
        {
            q = q.Where(u => u.ActivationStatus == activationStatus.Value);
        }

        switch (area)
        {
            case Area.People:
                q = q.Where(u =>
                    !filesDbContext.UserGroup.Any(ug =>
                        ug.TenantId == tenantId &&
                        ug.Userid == u.Id &&
                        ug.UserGroupId == Constants.GroupGuest.ID &&
                        ug.RefType == UserGroupRefType.Contains &&
                        !ug.Removed));
                break;
            case Area.Guests:
                {
                    q = q.Join(filesDbContext.UserGroup,
                        u => new
                        {
                            TenantId = tenantId,
                            Userid = u.Id,
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
                        (u, ug) => u);

                    if (!includeStrangers)
                    {
                        var currentUserId = _authContext.CurrentAccount.ID;
                        q = q.Join(filesDbContext.UserRelations,
                            u => new
                            {
                                TenantId = tenantId, 
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
                    }
                    break;
                }
        }

        q = UserQueryHelper.FilterByText(q, text, separator);
            
        var q1 = excludeShared
            ? q.Where(u => 
                    !filesDbContext.Security.Any(s => 
                        s.TenantId == tenantId &&
                        s.EntryId == entryId &&
                        s.EntryType == entry.FileEntryType &&
                        s.Subject == u.Id) && 
                    u.Id != entry.CreateBy)
                .Select(u => new UserWithShared { User = u, Shared = false })
            : q.GroupJoin(
                    filesDbContext.Security, 
                    user => new
                    {
                        TenantId = tenantId,
                        Subject = user.Id,
                        EntryId = entryId,
                        EntryType = entry.FileEntryType
                    },
                    security => new
                    {
                        security.TenantId,
                        security.Subject,
                        security.EntryId,
                        security.EntryType
                    },
                    (user, grouping) => new { user, grouping }
                )
                .SelectMany(
                    x => x.grouping.DefaultIfEmpty(), 
                    (x, s) => new { x.user, s }
                )
                .Select(x => new UserWithShared
                {
                    User = x.user, 
                    Shared = x.s != null || x.user.Id == entry.CreateBy
                });

        if (area == Area.All && !includeStrangers)
        {
            var currentUserId = _authContext.CurrentAccount.ID;
            
            q1 = q1.GroupJoin(filesDbContext.UserRelations,
                    us => new
                    {
                        TenantId = tenantId,
                        SourceUserId = currentUserId, 
                        TargetUserId = us.User.Id
                    },
                    ur => new
                    {
                        ur.TenantId,
                        ur.SourceUserId, 
                        ur.TargetUserId
                    },
                    (us, ur) => new { us, ur })
                .SelectMany(
                    x => x.ur.DefaultIfEmpty(),
                    (x, ur) => new { x.us, ur })
                .GroupJoin(filesDbContext.UserGroup,
                    x => new
                    {
                        TenantId = tenantId,
                        Userid = x.us.User.Id,
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
                    (x, ug) => new { x.us, x.ur, ug })
                .SelectMany(
                    x => x.ug.DefaultIfEmpty(),
                    (x, ug) => new { x.us, x.ur, ug })
                .Where(x => 
                    x.ug == null ||
                    x.us.Shared ||
                    (x.ur != null && x.ug != null))
                .Select(x => x.us);
        }
        
        q1 = q1
            .OrderBy(u => u.User.ActivationStatus)
            .ThenBy(u => u.User.FirstName);

        return q1;
    }

    internal async IAsyncEnumerable<FileShareRecord<T>> InternalGetPureShareRecordsAsync(FileEntry<T> entry)
    {
        var files = new List<string>();
        var folders = new List<string>();

        await SelectFilesAndFoldersForShareAsync(entry, files, folders, null);

        await foreach (var r in GetPureShareRecordsDbAsync(files, folders))
        {
            yield return r;
        }
    }

    private async IAsyncEnumerable<FileShareRecord<T>> GetPureShareRecordsDbAsync(IEnumerable<string> files, IEnumerable<string> folders)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = filesDbContext.PureShareRecordsDbAsync(tenantId, files, folders);

        await foreach (var e in q)
        {
            yield return await ToFileShareRecordAsync(e);
        }
    }

    public async Task RemoveBySubjectAsync(Guid subject, bool withoutOwner)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (withoutOwner)
        {
            await filesDbContext.RemoveBySubjectWithoutOwnerAsync(tenantId, subject);
        }
        else
        {
            await filesDbContext.RemoveBySubjectAsync(tenantId, subject);
        }

        await filesDbContext.SaveChangesAsync();
    }
    
    public async IAsyncEnumerable<FileShareRecord<T>> GetPureSharesAsync(FileEntry<T> entry, IEnumerable<Guid> subjects)
    {
        if (subjects == null || !subjects.Any())
        {
            yield break;
        }
        
        var entryId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var security in filesDbContext.EntrySharesBySubjectsAsync(tenantId, entryId, entry.FileEntryType, subjects))
        {
            yield return await ToFileShareRecordAsync(security);
        }
    }

    internal async Task SelectFilesAndFoldersForShareAsync(FileEntry<T> entry, ICollection<string> files, ICollection<string> folders, ICollection<int> foldersInt)
    {
        T folderId;
        if (entry.FileEntryType == FileEntryType.File)
        {
            var fileId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);
            folderId = ((File<T>)entry).ParentId;
            if (!files.Contains(fileId))
            {
                files.Add(fileId);
            }
        }
        else
        {
            folderId = entry.Id;
        }

        if (foldersInt != null && int.TryParse(folderId.ToString(), out var folderIdInt) && !foldersInt.Contains(folderIdInt))
        {
            foldersInt.Add(folderIdInt);
        }

        var mappedId = await daoFactory.GetMapping<T>().MappingIdAsync(folderId);
        folders?.Add(mappedId);
    }

    internal async Task<IQueryable<DbFilesSecurity>> GetQuery(FilesDbContext filesDbContext, Expression<Func<DbFilesSecurity, bool>> where = null)
    {
        var q = await Query(filesDbContext.Security);
        q = q?.Where(where);
        return q;
    }

    private async Task<FileShareRecord<T>> ToFileShareRecordAsync(DbFilesSecurity r)
    {
        var result = mapper.Map<DbFilesSecurity, FileShareRecord<T>>(r);
        result.EntryId = (T)Convert.ChangeType(await daoFactory.GetMapping<T>().MappingIdAsync(result.EntryId), typeof(T));
        return result;
    }

    protected FileShareRecord<T> ToFileShareRecord(SecurityTreeRecord r)
    {
        var result = mapper.Map<SecurityTreeRecord, FileShareRecord<T>>(r);

        if (r.FolderId != default)
        {
            result.EntryId = (T)Convert.ChangeType(r.FolderId, typeof(T));
        }

        return result;
    }
    
    private async Task<IQueryable<DbFilesSecurity>> GetPureSharesQuery(int tenantId, FileEntry<T> entry, ShareFilterType filterType, FilesDbContext filesDbContext)
    {
        var entryId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);

        var q = filesDbContext.Security.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.EntryId == entryId && s.EntryType == entry.FileEntryType);

        switch (filterType)
        {
            case ShareFilterType.UserOrGroup:
                q = q.Where(s => s.SubjectType == SubjectType.User || s.SubjectType == SubjectType.Group);
                break;
            case ShareFilterType.InvitationLink:
                q = q.Where(s => s.SubjectType == SubjectType.InvitationLink);
                break;
            case ShareFilterType.ExternalLink:
                q = q.Where(s => s.SubjectType == SubjectType.ExternalLink || s.SubjectType == SubjectType.PrimaryExternalLink);
                break;
            case ShareFilterType.AdditionalExternalLink:
                q = q.Where(s => s.SubjectType == SubjectType.ExternalLink);
                break;
            case ShareFilterType.PrimaryExternalLink:
                q = q.Where(s => s.SubjectType == SubjectType.PrimaryExternalLink);
                break;
            case ShareFilterType.Link:
                q = q.Where(s => s.SubjectType == SubjectType.InvitationLink || s.SubjectType == SubjectType.ExternalLink || s.SubjectType == SubjectType.PrimaryExternalLink);
                break;
            case ShareFilterType.Group:
                q = q.Where(s => s.SubjectType == SubjectType.Group);
                break;
            case ShareFilterType.User:
                q = q.Where(s => s.SubjectType == SubjectType.User);
                break;
        }

        return q;
    }

    protected async IAsyncEnumerable<FileShareRecord<T>> DeleteExpiredAsync(IAsyncEnumerable<FileShareRecord<T>> records, FilesDbContext filesDbContext)
    {
        var expired = new List<Guid>();
        
        await foreach (var r in records)
        {
            if (r.SubjectType == SubjectType.InvitationLink && r.Options is { IsExpired: true })
            {
                expired.Add(r.Subject);
                continue;
            }
            
            yield return r;
        }

        if (expired.Count <= 0)
        {
            yield break;
        }

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await filesDbContext.Security
            .Where(s => s.TenantId == tenantId && s.SubjectType == SubjectType.InvitationLink && expired.Contains(s.Subject))
            .ExecuteDeleteAsync();
    }
}

[Scope(typeof(ISecurityDao<int>))]
internal class SecurityDao(
    IDaoFactory daoFactory,
    UserManager userManager,
        IDbContextFactory<FilesDbContext> dbContextFactory,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        SetupInfo setupInfo,
        MaxTotalSizeStatistic maxTotalSizeStatistic,
        SettingsManager settingsManager,
        AuthContext authContext,
        IServiceProvider serviceProvider,
        IMapper mapper,
        IDistributedLockProvider distributedLockProvider)
    : SecurityBaseDao<int>(daoFactory, userManager, dbContextFactory, tenantManager, tenantUtil, setupInfo, maxTotalSizeStatistic, settingsManager, authContext, serviceProvider, mapper, distributedLockProvider), ISecurityDao<int>
{
    public async Task<IEnumerable<FileShareRecord<int>>> GetSharesAsync(FileEntry<int> entry, IEnumerable<Guid> subjects = null)
    {
        if (entry == null)
        {
            return [];
        }

        var files = new List<string>();
        var foldersInt = new List<int>();

        await SelectFilesAndFoldersForShareAsync(entry, files, null, foldersInt);

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q = (await Query(filesDbContext.Security))
            .Join(filesDbContext.Tree, r => r.EntryId, a => a.ParentId.ToString(), 
                (s, t) => new SecurityTreeRecord
                {
                    TenantId = s.TenantId,
                    EntryId = s.EntryId,
                    EntryType = s.EntryType,
                    SubjectType = s.SubjectType,
                    Subject = s.Subject,
                    Owner = s.Owner,
                    Share = s.Share,
                    TimeStamp = s.TimeStamp,
                    Options = s.Options,
                    FolderId = t.FolderId,
                    ParentId = t.ParentId,
                    Level = t.Level
                })
            .Where(r => foldersInt.Contains(r.FolderId) && r.EntryType == FileEntryType.Folder);

        if (files.Count > 0)
        {
            var q1 = (await GetQuery(filesDbContext, r => files.Contains(r.EntryId) && r.EntryType == FileEntryType.File))
                .Select(s => new SecurityTreeRecord
                {
                    TenantId = s.TenantId,
                    EntryId = s.EntryId,
                    EntryType = s.EntryType,
                    SubjectType = s.SubjectType,
                    Subject = s.Subject,
                    Owner = s.Owner,
                    Share = s.Share,
                    TimeStamp = s.TimeStamp,
                    Options = s.Options,
                    FolderId = 0,
                    ParentId = 0,
                    Level = -1
                });
            
            q = q.Concat(q1);
        }

        if (subjects != null && subjects.Any())
        {
            q = q.Where(r => subjects.Contains(r.Subject));
        }

        var records = q.ToAsyncEnumerable()
            .Select(ToFileShareRecord)
            .OrderBy(r => r.Level)
            .ThenByDescending(r => r.Share, new FileShareRecord<int>.ShareComparer(entry.RootFolderType));

        return await DeleteExpiredAsync(records, filesDbContext).ToListAsync();
    }

    public async Task<bool> IsSharedAsync(FileEntry<int> entry, IEnumerable<SubjectType> subjectTypes)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (entry.RootFolderType is FolderType.USER)
        {
            return entry.FileEntryType is FileEntryType.File && 
                   await filesDbContext.IsPureSharedAsync(tenantId, entry.Id.ToString(), FileEntryType.File, subjectTypes);
}

        if (entry.RootFolderType is not FolderType.VirtualRooms)
        {
            return false;
        }

        if (entry is Folder<int> folder && DocSpaceHelper.IsRoom(folder.FolderType))
        {
            return await filesDbContext.IsPureSharedAsync(tenantId, entry.Id.ToString(), FileEntryType.Folder, subjectTypes);
        }

        return await filesDbContext.IsSharedAsync(tenantId, entry.ParentId, subjectTypes);
    }
}

[Scope(typeof(ISecurityDao<string>))]
internal class ThirdPartySecurityDao(
    IDaoFactory daoFactory,
    UserManager userManager,
        IDbContextFactory<FilesDbContext> dbContextFactory,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        SetupInfo setupInfo,
        MaxTotalSizeStatistic maxTotalSizeStatistic,
        SettingsManager settingsManager,
        AuthContext authContext,
        IServiceProvider serviceProvider,
        IMapper mapper,
        SelectorFactory selectorFactory,
        IDistributedLockProvider distributedLockProvider)
    : SecurityBaseDao<string>(daoFactory, userManager, dbContextFactory, tenantManager, tenantUtil, setupInfo, maxTotalSizeStatistic, settingsManager, authContext, serviceProvider, mapper, distributedLockProvider), ISecurityDao<string>
{
    public async Task<IEnumerable<FileShareRecord<string>>> GetSharesAsync(FileEntry<string> entry, IEnumerable<Guid> subjects = null)
    {
        var result = new List<FileShareRecord<string>>();

        var folders = new List<FileEntry<string>>();
        if (entry is Folder<string> entryFolder)
        {
            folders.Add(entryFolder);
        }

        if (entry is File<string> file)
        {
            await GetFoldersForShareAsync(file.ParentId, folders);

            var pureShareRecords = GetPureShareRecordsAsync(entry);
            await foreach (var pureShareRecord in pureShareRecords)
            {
                if (pureShareRecord == null)
                {
                    continue;
                }

                pureShareRecord.Level = -1;
                result.Add(pureShareRecord);
            }
        }

        result.AddRange(await GetShareForFoldersAsync(folders).ToListAsync());

        if (subjects != null && subjects.Any())
        {
            result = result.Where(r => subjects.Contains(r.Subject)).ToList();
        }

        return result;
    }

    public Task<bool> IsSharedAsync(FileEntry<string> entry, IEnumerable<SubjectType> subjectTypes)
    {
        return Task.FromResult(entry.RootFolderType is FolderType.VirtualRooms);
    }

    private async ValueTask GetFoldersForShareAsync(string folderId, ICollection<FileEntry<string>> folders)
    {
        var selector = selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);
        if (folderDao == null)
        {
            return;
        }

        var folder = await folderDao.GetFolderAsync(selector.ConvertId(folderId));

        if (folder != null)
        {
            folders.Add(folder);
        }
    }

    private async IAsyncEnumerable<FileShareRecord<string>> GetShareForFoldersAsync(IEnumerable<FileEntry<string>> folders)
    {
        foreach (var folder in folders)
        {
            var selector = selectorFactory.GetSelector(folder.Id);
            var folderDao = selector.GetFolderDao(folder.Id);
            if (folderDao == null)
            {
                continue;
            }

            var parentFolders = await folderDao.GetParentFoldersAsync(selector.ConvertId(folder.Id)).ToListAsync();
            if (parentFolders.Count == 0)
            {
                continue;
            }

            parentFolders.Reverse();
            var pureShareRecords = await GetPureShareRecordsAsync(parentFolders).ToListAsync();

            foreach (var pureShareRecord in pureShareRecords)
            {
                if (pureShareRecord == null)
                {
                    continue;
                }

                var f = _serviceProvider.GetService<Folder<string>>();
                f.Id = pureShareRecord.EntryId;

                pureShareRecord.Level = parentFolders.IndexOf(f);
                pureShareRecord.EntryId = folder.Id;
                yield return pureShareRecord;
            }
        }
    }
} 
internal class SecurityTreeRecord
{
    public int TenantId { get; set; }
    public string EntryId { get; set; }
    public FileEntryType EntryType { get; init; }
    public SubjectType SubjectType { get; set; }
    public Guid Subject { get; init; }
    public Guid Owner { get; set; }
    public FileShare Share { get; set; }
    public DateTime TimeStamp { get; set; }
    public string Options { get; init; }
    public int FolderId { get; init; }
    public int ParentId { get; set; }
    public int Level { get; set; }
}

public class SecurityUserRecord
{
    public DbFilesSecurity Security { get; init; }
    public User User { get; init; }
}

public class SecurityOrderRecord
{
    public DbFilesSecurity Security { get; init; }
    public int Order { get; init; }
    public string Name { get; init; }
}

public class UserInfoWithShared
{
    public UserInfo UserInfo { get; init; }
    public bool Shared { get; init; }
}

public class GroupInfoWithShared
{
    public GroupInfo GroupInfo { get; init; }
    public bool Shared { get; init; }
}

public class UserWithShared
{
    public User User { get; init; }
    public bool Shared { get; init; }
}

public class GroupWithShared
{
    public DbGroup Group { get; init; }
    public bool Shared { get; init; }
}