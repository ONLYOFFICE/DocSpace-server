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

[Scope(typeof(IAzService), typeof(CachedAzService))]
class DbAzService(IDbContextFactory<UserDbContext> dbContextFactory, IMapper mapper) : IAzService
{
    public async Task<IEnumerable<AzRecord>> GetAcesAsync(int tenant, DateTime from)
    {
        await using var userDbContext = await dbContextFactory.CreateDbContextAsync();

        // row with tenant = -1 - common for all tenants, but equal row with tenant != -1 escape common row for the portal
        var commonAces = await userDbContext.AzRecordAsync()
            .Select(mapper.Map<Acl, AzRecord>)
            .ToDictionaryAsync(a => string.Concat(a.TenantId.ToString(), a.Subject.ToString(), a.Action.ToString(), a.Object));

        var tenantAces = await
            userDbContext.Acl
            .Where(r => r.TenantId == tenant)
            .ProjectTo<AzRecord>(mapper.ConfigurationProvider)
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
        await userDbContext.AddOrUpdateAsync(q => q.Acl, mapper.Map<AzRecord, Acl>(r));
        await userDbContext.SaveChangesAsync();
    }
}
