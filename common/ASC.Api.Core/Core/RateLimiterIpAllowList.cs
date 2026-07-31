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

using IPNetwork = System.Net.IPNetwork;

namespace ASC.Api.Core;

public class RateLimiterIpAllowList
{
    private const string CacheKey = "rateLimiterIpAllowList";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IFusionCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RateLimiterIpAllowList> _logger;
    private readonly string _url;
    private readonly TimeSpan _refreshInterval;
    private readonly Snapshot _static;

    public RateLimiterIpAllowList(IOptions<RateLimiterSettings> settings, IFusionCacheProvider cacheProvider, IHttpClientFactory httpClientFactory, ILogger<RateLimiterIpAllowList> logger)
    {
        _cache = cacheProvider.GetMemoryCache();
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _url = settings.Value.KnownIPAddressesUrl;
        _refreshInterval = TimeSpan.FromMinutes(Math.Max(1, settings.Value.KnownIPAddressesRefreshMinutes));
        _static = Parse(settings.Value.KnownIPAddresses.Concat(settings.Value.KnownNetworks));
    }

    public bool Contains(IPAddress address)
    {
        if (address == null)
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        return Contains(_static, address) || Contains(GetRemote(), address);
    }

    private Snapshot GetRemote()
    {
        if (string.IsNullOrEmpty(_url))
        {
            return Snapshot.Empty;
        }

        return _cache.GetOrSet<Snapshot>(CacheKey,
            Fetch,
            Snapshot.Empty,
            opt => opt
                .SetDuration(_refreshInterval)
                .SetFailSafe(true)
                .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)));
    }

    private Snapshot Fetch(FusionCacheFactoryExecutionContext<Snapshot> ctx, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, _url);

            if (ctx is { HasStaleValue: true, HasETag: true })
            {
                request.Headers.TryAddWithoutValidation(HeaderNames.IfNoneMatch, ctx.ETag);
            }

            using var response = httpClient.Send(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                _logger.DebugAllowListNotModified(_url, ctx.ETag);
                return ctx.NotModified();
            }

            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStream(cancellationToken);
            var regions = JsonSerializer.Deserialize<Dictionary<string, RemoteIpListNode>>(stream, _jsonOptions) ?? [];

            var updatedAt = regions.Count > 0 ? regions.Values.Max(r => r.UpdatedAt).ToString("O") : string.Empty;

            var snapshot = Parse(regions.Values.SelectMany(r => r.Ips ?? []));
            _logger.InformationAllowListRefreshed(_url, snapshot.Addresses.Count + snapshot.Networks.Length, updatedAt);

            return ctx.Modified(snapshot, etag: response.Headers.ETag?.ToString());
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.WarningAllowListRefreshFailed(_url, e);
            throw;
        }
    }

    private Snapshot Parse(IEnumerable<string> entries)
    {
        var addresses = new HashSet<IPAddress>();
        var networks = new List<IPNetwork>();

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            var trimmed = entry.Trim();

            if (trimmed.Contains('/'))
            {
                if (IPNetwork.TryParse(trimmed, out var network) && network.BaseAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    networks.Add(network);
                    continue;
                }
            }
            else if (IPAddress.TryParse(trimmed, out var address) && address.AddressFamily == AddressFamily.InterNetwork)
            {
                addresses.Add(address);
                continue;
            }

            _logger.DebugInvalidAllowListEntry(entry);
        }

        return new Snapshot(addresses, [.. networks]);
    }

    private static bool Contains(Snapshot snapshot, IPAddress address)
    {
        if (snapshot.Addresses.Contains(address))
        {
            return true;
        }

        foreach (var network in snapshot.Networks)
        {
            if (network.Contains(address))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record Snapshot(HashSet<IPAddress> Addresses, IPNetwork[] Networks)
    {
        public static readonly Snapshot Empty = new([], []);
    }

    private sealed record RemoteIpListNode(
        List<string> Ips,
        [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt);
}
