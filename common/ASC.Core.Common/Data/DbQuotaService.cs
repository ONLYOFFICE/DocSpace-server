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

namespace ASC.Core.Data;

[Scope]
class DbQuotaService(IDbContextFactory<CoreDbContext> dbContextManager, IMapper mapper)
    : IQuotaService
{
    public async Task<IEnumerable<TenantQuota>> GetTenantQuotasAsync()
    {
        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();
        var res = await coreDbContext.AllQuotasAsync().ToListAsync();
        return mapper.Map<List<DbQuota>, List<TenantQuota>>(res);
    }

    public async Task<TenantQuota> GetTenantQuotaAsync(int id)
    {
        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();

        return mapper.Map<DbQuota, TenantQuota>(await coreDbContext.Quotas.SingleOrDefaultAsync(r => r.TenantId == id));
    }

    public async Task<TenantQuota> SaveTenantQuotaAsync(TenantQuota quota)
    {
        ArgumentNullException.ThrowIfNull(quota);

        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();
        await coreDbContext.AddOrUpdateAsync(q => q.Quotas, mapper.Map<TenantQuota, DbQuota>(quota));
        await coreDbContext.SaveChangesWithValidateAsync();

        return quota;
    }

    public async Task RemoveTenantQuotaAsync(int id)
    {
        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();

        var quota = await coreDbContext.QuotaAsync(id);

        if (quota != null)
        {
            coreDbContext.Quotas.Remove(quota);
            await coreDbContext.SaveChangesWithValidateAsync();
        }
    }


    public async Task SetTenantQuotaRowAsync(TenantQuotaRow row, bool exchange)
    {
        ArgumentNullException.ThrowIfNull(row);

        await using var coreDbContext = await dbContextManager.CreateDbContextAsync();
        var dbTenantQuotaRow = mapper.Map<TenantQuotaRow, DbQuotaRow>(row);
        dbTenantQuotaRow.UserId = row.UserId;

        var exist = await coreDbContext.QuotaRows.FindAsync(dbTenantQuotaRow.TenantId, dbTenantQuotaRow.UserId, dbTenantQuotaRow.Path);

        if (exist == null)
        {
            await coreDbContext.QuotaRows.AddAsync(dbTenantQuotaRow);
            await coreDbContext.SaveChangesWithValidateAsync();
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
                await coreDbContext.SaveChangesWithValidateAsync();
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

        return await q.ProjectTo<TenantQuotaRow>(mapper.ConfigurationProvider).ToListAsync();
    }
}