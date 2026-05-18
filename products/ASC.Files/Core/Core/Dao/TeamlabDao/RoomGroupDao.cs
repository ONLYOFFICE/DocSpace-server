// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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
