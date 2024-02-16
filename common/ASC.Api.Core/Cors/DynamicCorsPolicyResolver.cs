// (c) Copyright Ascensio System SIA 2010-2023
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

using System.Text.Json.Nodes;

using ASC.Api.Core.Cors.Resolvers;
using ASC.Core.Security.Authentication;
using ASC.Web.Studio.Core;

using Microsoft.Extensions.Caching.Memory;

namespace ASC.Api.Core.Cors;
public class DynamicCorsPolicyResolver : IDynamicCorsPolicyResolver
{
    private readonly HttpContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CookieStorage _cookieStorage;
    private readonly CookiesManager _cookiesManager;
    private readonly TenantManager _tenantManager;
    private readonly string _apiBaseUrl;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<DynamicCorsPolicyResolver> _logger;

    public DynamicCorsPolicyResolver(IHttpContextAccessor httpContextAccessor,
                                     IHttpClientFactory httpClientFactory,
                                     CookieStorage cookieStorage,
                                     CookiesManager cookiesManager,
                                     TenantManager tenantManager,
                                     CommonLinkUtility linkUtility,
                                     SetupInfo setupInfo,
                                     IMemoryCache memoryCache,
                                     ILogger<DynamicCorsPolicyResolver> logger)
    {
        _context = httpContextAccessor?.HttpContext;
        _httpClientFactory = httpClientFactory;
        _cookieStorage = cookieStorage;
        _cookiesManager = cookiesManager;
        _tenantManager = tenantManager;
        _memoryCache = memoryCache;
        _apiBaseUrl = setupInfo.WebApiBaseUrl;

        if (Uri.IsWellFormedUriString(_apiBaseUrl, UriKind.Relative))
        {
            _apiBaseUrl = linkUtility.GetFullAbsolutePath(_apiBaseUrl);
        }

        _logger = logger;   
    }

    public async Task<bool> ResolveForOrigin(string origin)
    {
        _logger.DebugCheckOrigin(origin);

        var origins = await GetOriginsFromOAuth2App();

        return origins.Any(x => x.Equals(origin, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task<IEnumerable<string>> GetOriginsFromOAuth2App()
    {        
        var accessToken = _context.Request.Headers["Authorization"].ToString();

        if (accessToken == null || accessToken.IndexOf("Bearer", 0) == -1)
        {
            return new List<string>();
        }

        accessToken = accessToken.Trim();
        accessToken = accessToken["Bearer ".Length..];

        // Validated token early in JwtBearerAuthHandler
        var token = new JwtSecurityToken(accessToken);
        var subject = token.Subject;
        var userId = Guid.Parse(subject);

        if (!Guid.TryParse(subject, out userId))
        {
            return new List<string>();
        }

        var claimIdClaim = token.Claims.Single(c => string.Equals(c.Type, "cid", StringComparison.OrdinalIgnoreCase));
        var clientId = Guid.Parse(claimIdClaim.Value);

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var cookieValue = await _cookieStorage.EncryptCookieAsync(tenantId, userId, 0);
        var cookieName = _cookiesManager.GetAscCookiesName();

        var origins = _memoryCache.Get<IEnumerable<string>>(clientId);

        if (origins == null)
        {
            using var httpClient = _httpClientFactory.CreateClient();

            var requestUri = new Uri($"{_apiBaseUrl}clients/{clientId}");

            httpClient.DefaultRequestHeaders.Add("Cookie", $"{cookieName}={cookieValue}");

            var httpResponse = await httpClient.GetStringAsync(requestUri);

            var forecastNode = JsonNode.Parse(httpResponse)!;

            origins = forecastNode!["allowed_origins"]!.AsArray().Select(x => x.GetValue<string>());

            _memoryCache.Set(clientId, origins, TimeSpan.FromMinutes(15));
        }

        _logger.DebugGetOriginsFromOAuth2App(clientId, origins);

        return origins;
    }

}
