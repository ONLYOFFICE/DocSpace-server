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

[Scope(typeof(IAzService), typeof(CachedAzService))]
internal class DbAzService(IDbContextFactory<UserDbContext> dbContextFactory) : IAzService
{
    public async Task<IEnumerable<AzRecord>> GetAcesAsync(int tenant, DateTime from)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        // row with tenant = -1 - common for all tenants, but equal row with tenant != -1 escape common row for the portal
        var commonAces = await userDbContext.AzRecordAsync()
            .Select(r => r.Map())
            .ToDictionaryAsync(a => string.Concat(a.TenantId.ToString(), a.Subject.ToString(), a.Action.ToString(), a.Object));

        var tenantAces = await
            userDbContext.Acl
            .Where(r => r.TenantId == tenant)
            .Project()
            .ToListAsync();

        // remove excaped rows
        foreach (var a in tenantAces)
        {
            var key = string.Concat(a.TenantId.ToString(), a.Subject.ToString(), a.Action.ToString(), a.Object);
            if (commonAces.Remove(key, out var common))
            {
                if (common.AceType == a.AceType)
                {
                    tenantAces.Remove(a);
                }
            }
        }

        return commonAces.Values.Concat(tenantAces);
    }

    public async Task<AzRecord> SaveAceAsync(int tenant, AzRecord r)
    {
        r.TenantId = tenant;

        if (!await ExistEscapeRecordAsync(r))
        {
            await InsertRecordAsync(r);
        }
        else
        {
            // unescape
            await DeleteRecordAsync(r);
        }

        return r;
    }

    public async Task RemoveAceAsync(int tenant, AzRecord r)
    {
        r.TenantId = tenant;

        if (await ExistEscapeRecordAsync(r))
        {
            // escape
            await InsertRecordAsync(r);
        }
        else
        {
            await DeleteRecordAsync(r);
        }

    }


    private async Task<bool> ExistEscapeRecordAsync(AzRecord r)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        return await userDbContext.AnyAclAsync(Tenant.DefaultTenant, r.Subject, r.Action, r.Object ?? string.Empty, r.AceType);
    }

    private async Task DeleteRecordAsync(AzRecord r)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        var record = await userDbContext.AclAsync(r.TenantId, r.Subject, r.Action, r.Object ?? string.Empty, r.AceType);

        if (record != null)
        {
            userDbContext.Acl.Remove(record);
            await userDbContext.SaveChangesAsync();
        }
    }

    private async Task InsertRecordAsync(AzRecord r)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();
        await userDbContext.AddOrUpdateAsync(q => q.Acl, r.Map());
        await userDbContext.SaveChangesAsync();
    }
}