// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Core.Data;

[Scope(typeof(IRoomGroupDao<int>), GenericArguments = [typeof(int)])]
[Scope(typeof(IRoomGroupDao<string>), GenericArguments = [typeof(string)])]
internal class RoomGroupDao<T>(
    UserManager userManager,
    IDbContextFactory<FilesDbContext> dbContextManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    SetupInfo setupInfo,
    MaxTotalSizeStatistic maxTotalSizeStatistic,
    SettingsManager settingsManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider)
    : AbstractDao(dbContextManager,
        userManager,
        tenantManager,
        tenantUtil,
        setupInfo,
        maxTotalSizeStatistic,
        settingsManager,
        authContext,
        serviceProvider,
        distributedLockProvider), IRoomGroupDao<T>
{
    public async Task<RoomGroup> SaveRoomGroupAsync(RoomGroup groupInfo)
    {
        ArgumentNullException.ThrowIfNull(groupInfo);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var tenantId = _tenantManager.GetCurrentTenantId();

        var entity = groupInfo.Id != 0
            ? await dbContext.GroupForUpdateAsync(tenantId, groupInfo.Id)
            : new DbFilesGroup { TenantId = tenantId, UserId = groupInfo.UserID };

        entity.Name = groupInfo.Name;
        entity.Icon = groupInfo.Icon;

        var newGroup = await dbContext.AddOrUpdateAsync(q => q.RoomGroup, entity);
        await dbContext.SaveChangesAsync();

        return newGroup.MapToRoomGroup();
    }

    public async Task<RoomGroup> GetGroupInfoAsync(int roomGroupId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var tenantId = _tenantManager.GetCurrentTenantId();
        var group = await GetGroupQuery(dbContext, tenantId)
            .Where(r => r.Id == roomGroupId)
            .Where(r => r.UserId == _authContext.CurrentAccount.ID)
            .FirstOrDefaultAsync();

        return group.MapToRoomGroup();
    }
    public async Task DeleteGroup(int groupId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var tenantId = _tenantManager.GetCurrentTenantId();

        var group = await GetGroupQuery(dbContext, tenantId)
            .Where(r => r.Id == groupId)
            .Where(r => r.UserId == _authContext.CurrentAccount.ID)
            .FirstOrDefaultAsync();

        if (group == null)
        {
            return;
        }
        dbContext.RoomGroup.Remove(group);
        await dbContext.SaveChangesAsync();
    }

    public async IAsyncEnumerable<RoomGroup> GetGroupsAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var tenantId = _tenantManager.GetCurrentTenantId();

        var query = GetGroupQuery(dbContext, tenantId)
            .Where(r => r.UserId == _authContext.CurrentAccount.ID)
            .AsAsyncEnumerable();

        await foreach (var item in query)
        {
            yield return item.MapToRoomGroup();
        }
    }

    private IQueryable<DbFilesGroup> GetGroupQuery(FilesDbContext dbContext, int tenant)
    {
        var q = dbContext.RoomGroup.AsQueryable();

        if (tenant != Tenant.DefaultTenant)
        {
            q = q.Where(r => r.TenantId == tenant);
        }

        return q;
    }

    public async Task AddRoomToGroupAsync(T roomId, int groupId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var tenantId = _tenantManager.GetCurrentTenantId();

        int? internalRoomId = null;
        string thirdpartyRoomId = null;

        if (roomId is int intRoomId)
        {
            if (await db.AnyRoomGroupRefAsync(tenantId, groupId, intRoomId)) 
            {
                return;
            }
            internalRoomId = intRoomId;
        }

        if (roomId is string stringRoomId)
        {
            if (await db.AnyRoomGroupRefAsync(tenantId, groupId, stringRoomId))
            {
                return;
            }
            thirdpartyRoomId = stringRoomId;
        }

        if (internalRoomId == null && thirdpartyRoomId == null)
        {
            throw new InvalidOperationException();
        }
        
        await db.AddOrUpdateAsync(
            q => q.RoomGroupRef,
            new DbFilesRoomGroup
            {
                TenantId = tenantId,
                GroupId = groupId,
                InternalRoomId = internalRoomId,
                ThirdpartyRoomId = thirdpartyRoomId
            });

        await db.SaveChangesAsync();
    }

    public async Task RemoveRoomFromGroupAsync(T roomId, int groupId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var tenantId = _tenantManager.GetCurrentTenantId();

        DbFilesRoomGroup entity = null;

        if (roomId is int intRoomId)
        {
            entity = await db.RoomGroupRef.FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.GroupId == groupId &&
                r.InternalRoomId == intRoomId);
        }

        if (roomId is string stringRoomId)
        {
            entity = await db.RoomGroupRef.FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.GroupId == groupId &&
                r.ThirdpartyRoomId == stringRoomId);
        }

        if (entity == null) 
        {
            return;
        }

        db.RoomGroupRef.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async IAsyncEnumerable<RoomGroupRef> GetRoomsByGroupAsync(int groupId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var roomGroupDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var data in roomGroupDbContext.GetRoomsByGroupAsync(tenantId, groupId))
        {
            yield return data.MapToRoomGroupRef();
        }
    }
}
