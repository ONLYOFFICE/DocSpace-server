// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Core.Caching;

[Scope]
class CachedAzService(DbAzService service, IFusionCacheProvider cacheProvider) : IAzService
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
        }, opt => opt.SetDuration(_cacheExpiration).SetFailSafe(true));

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
