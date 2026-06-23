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

namespace ASC.IPSecurity;

[Scope]
public class IPRestrictionsService(IPRestrictionsRepository iPRestrictionsRepository, IFusionCache fusionCache)
{
    private const string CacheKey = "iprestrictions";
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    public async Task<List<IPRestriction>> GetAsync(int tenant, string etagFromRequest = null)
    {
        var key = CacheKey + tenant;
        return await fusionCache.GetOrSetAsync<List<IPRestriction>>(key, async (ctx, token) =>
            {
                if (!string.IsNullOrEmpty(etagFromRequest) && ctx is { HasStaleValue: true, HasETag: true } && ctx.ETag == etagFromRequest)
                {
                    return ctx.NotModified();
                }

                var result = await iPRestrictionsRepository.GetAsync(tenant, token);
                var etag = await CalculateEtagAsync(result, token);

                return ctx.Modified(result, etag: etag);
            },
            opt => opt.SetDuration(_timeout).SetFailSafe(true));
    }

    public async Task<IEnumerable<IpRestrictionBase>> SaveAsync(IEnumerable<IpRestrictionBase> ips, int tenant)
    {
        var restrictions = await iPRestrictionsRepository.SaveAsync(ips, tenant);
        await fusionCache.RemoveAsync(CacheKey + tenant);
        return restrictions;
    }

    public async Task<string> CalculateEtagAsync(IEnumerable<IpRestrictionBase> ips, CancellationToken token = default)
    {
        using var md5 = MD5.Create();
        using var memoryStream = new MemoryStream();

        foreach (var restriction in ips)
        {
            var ip = IPAddress.Parse(restriction.Ip);
            var ipBytes = ip.GetAddressBytes();
            await memoryStream.WriteAsync(ipBytes, token);
        }

        var hash = await md5.ComputeHashAsync(memoryStream, token);
        var hex = BitConverter.ToString(hash);
        return hex.Replace("-", "");
    }
}