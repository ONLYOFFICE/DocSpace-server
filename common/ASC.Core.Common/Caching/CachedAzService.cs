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

namespace ASC.Core.Caching;

[Scope]
internal class CachedAzService(DbAzService service, IFusionCacheProvider cacheProvider) : IAzService
{
    private readonly DbAzService _service = service ?? throw new ArgumentNullException(nameof(service));
    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    private string GetKey(int tenant)
    {
        return "acl" + tenant;
    }

    public async Task<IEnumerable<AzRecord>> GetAcesAsync(int tenant, DateTime from)
    {
        var key = GetKey(tenant);
        var aces = await _cache.GetOrSetAsync<IEnumerable<AzRecord>>(key, async (ctx, token) =>
        {
            var records = await _service.GetAcesAsync(tenant, default);
            var aces = new AzRecordStore(records);
            return ctx.Modified(aces);
        }, _cacheExpiration);

        return aces;
    }

    public async Task<AzRecord> SaveAceAsync(int tenant, AzRecord r)
    {
        r = await _service.SaveAceAsync(tenant, r);
        await _cache.RemoveAsync(GetKey(tenant));

        return r;
    }

    public async Task RemoveAceAsync(int tenant, AzRecord r)
    {
        await _service.RemoveAceAsync(tenant, r);
        await _cache.RemoveAsync(GetKey(tenant));
    }
}