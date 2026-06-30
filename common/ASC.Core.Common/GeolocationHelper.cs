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

using System.Net.Sockets;

namespace ASC.Geolocation;

// hack for EF Core
public static class EntityFrameworkHelper
{
    public static int Compare(this byte[] b1, byte[] b2)
    {
        throw new Exception("This method can only be used in EF LINQ Context");
    }
}

[Scope]
public class GeolocationHelper(IDbContextFactory<CustomDbContext> dbContextFactory,
    ILogger<GeolocationHelper> logger,
    IHttpContextAccessor httpContextAccessor,
    ICache cache)
{
    public async Task<BaseEvent> AddGeolocationAsync(BaseEvent baseEvent)
    {
        var location = await GetGeolocationAsync(baseEvent.IP);
        baseEvent.Country = location[0];
        baseEvent.City = location[1];
        return baseEvent;
    }

    public async Task<string[]> GetGeolocationAsync(string ip)
    {
        try
        {
            if (!IPAddress.TryParse(ip, out var address))
            {
                return [string.Empty, string.Empty];
            }
            var location = await GetIPGeolocationAsync(address);
            if (string.IsNullOrEmpty(location.Key) || location.Key == "ZZ")
            {
                return [string.Empty, string.Empty];
            }
            var regionInfo = new RegionInfo(location.Key).EnglishName;
            return [regionInfo, location.City];
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            return [string.Empty, string.Empty];
        }
    }

    private static readonly TimeSpan _geolocationCacheTtl = TimeSpan.FromHours(1);

    public async Task<IPGeolocationInfo> GetIPGeolocationAsync(IPAddress address)
    {
        try
        {
            var cacheKey = $"ip_geolocation_info_{address}";
            var fromCache = cache.Get<IPGeolocationInfo>(cacheKey);

            if (fromCache != null)
            {
                return fromCache;
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var addrType = address.AddressFamily == AddressFamily.InterNetwork ? "ipv4" : "ipv6";

            var result = await Queries.IpGeolocationInfoAsync(dbContext, addrType, address.GetAddressBytes())
                         ?? IPGeolocationInfo.Default;

            cache.Insert(cacheKey, result, _geolocationCacheTtl);

            return result;
        }
        catch (Exception error)
        {
            logger.ErrorGetIPGeolocation(error);
        }

        return IPGeolocationInfo.Default;
    }

    public async Task<IPGeolocationInfo> GetIPGeolocationFromHttpContextAsync()
    {
        if (httpContextAccessor.HttpContext?.Request != null)
        {
            var ip = httpContextAccessor.HttpContext.Connection.RemoteIpAddress;

            if (ip != null && !ip.Equals(IPAddress.Loopback))
            {
                logger.TraceRemoteIpAddress(ip.ToString());

                return await GetIPGeolocationAsync(ip);
            }
        }

        return IPGeolocationInfo.Default;
    }
}

static file class Queries
{
    public static readonly Func<CustomDbContext, string, byte[], Task<IPGeolocationInfo>> IpGeolocationInfoAsync =
        EF.CompileAsyncQuery(
            (CustomDbContext ctx, string addrType, byte[] address) =>
                ctx.DbIPLookup
                    .Where(r => r.AddrType == addrType && r.IPStart.Compare(address) <= 0)
                    .OrderByDescending(r => r.IPStart)
                    .Select(r => new IPGeolocationInfo
                    {
                        City = r.City,
                        IPEnd = new IPAddress(r.IPEnd),
                        IPStart = new IPAddress(r.IPStart),
                        Key = r.Country,
                        TimezoneOffset = r.TimezoneOffset,
                        TimezoneName = r.TimezoneName,
                        Continent = r.Continent
                    })
                    .FirstOrDefault());
}
