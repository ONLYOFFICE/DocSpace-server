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

namespace ASC.Core.Data;

[Scope]
internal class DbQuotaService(IDbContextFactory<CoreDbContext> dbContextManager, TenantQuotaMapper tenantQuotaMapper) : IQuotaService
{
    public async Task<IEnumerable<TenantQuota>> GetTenantQuotasAsync()
    {
        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();
        var res = await coreDbContext.AllQuotasAsync().ToListAsync();
        return tenantQuotaMapper.Map(res);
    }

    public async Task<TenantQuota> GetTenantQuotaAsync(int id)
    {
        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();

        return await tenantQuotaMapper.MapDbQuotaToTenantQuota(await coreDbContext.Quotas.SingleOrDefaultAsync(r => r.TenantId == id));
    }

    public async Task<TenantQuota> SaveTenantQuotaAsync(TenantQuota quota)
    {
        ArgumentNullException.ThrowIfNull(quota);

        try
        {
            await using var coreDbContext = await dbContextManager.CreateDbContextAsync();
            await coreDbContext.AddOrUpdateAsync(q => q.Quotas, tenantQuotaMapper.Map(quota));
            await coreDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            await using var coreDbContext = await dbContextManager.CreateDbContextAsync();
            var fromDb = await coreDbContext.QuotaAsync(quota.TenantId);
            if (fromDb != null)
            {
                return await tenantQuotaMapper.MapDbQuotaToTenantQuota(fromDb);
            }
        }


        return quota;
    }

    public async Task RemoveTenantQuotaAsync(int id)
    {
        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();

        var quota = await coreDbContext.QuotaAsync(id);

        if (quota != null)
        {
            coreDbContext.Quotas.Remove(quota);
            await coreDbContext.SaveChangesAsync();
        }
    }


    public async Task SetTenantQuotaRowAsync(TenantQuotaRow row, bool exchange)
    {
        ArgumentNullException.ThrowIfNull(row);

        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();
        var dbTenantQuotaRow = row.Map();
        dbTenantQuotaRow.UserId = row.UserId;

        var exist = await coreDbContext.QuotaRows.FindAsync(dbTenantQuotaRow.TenantId, dbTenantQuotaRow.UserId, dbTenantQuotaRow.Path);

        if (exist == null)
        {
            await coreDbContext.QuotaRows.AddAsync(dbTenantQuotaRow);
            await coreDbContext.SaveChangesAsync();
        }
        else
        {
            if (exchange)
            {
                await coreDbContext.UpdateCounterAsync(row.TenantId, row.UserId, row.Path, row.Counter);
            }
            else
            {
                await coreDbContext.AddOrUpdateAsync(q => q.QuotaRows, dbTenantQuotaRow);
                await coreDbContext.SaveChangesAsync();
            }
        }
    }

    public async Task<IEnumerable<TenantQuotaRow>> FindTenantQuotaRowsAsync(int tenantId)
    {
        return await FindUserQuotaRowsAsync(tenantId, Guid.Empty);
    }

    public async Task<IEnumerable<TenantQuotaRow>> FindUserQuotaRowsAsync(int tenantId, Guid userId)
    {
        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();
        var q = coreDbContext.QuotaRows.Where(r => r.UserId == userId);

        if (tenantId != Tenant.DefaultTenant)
        {
            q = q.Where(r => r.TenantId == tenantId);
        }

        return await q.Project().ToListAsync();
    }
}
