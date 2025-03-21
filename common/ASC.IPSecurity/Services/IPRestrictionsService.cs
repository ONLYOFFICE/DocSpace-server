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
