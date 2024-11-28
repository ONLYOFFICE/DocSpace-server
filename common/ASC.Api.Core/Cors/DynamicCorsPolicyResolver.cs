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
public class DynamicCorsPolicyResolver(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    CookieStorage cookieStorage,
    CookiesManager cookiesManager,
    TenantManager tenantManager,
    CommonLinkUtility linkUtility,
    SetupInfo setupInfo,
    IMemoryCache memoryCache,
    ILogger<DynamicCorsPolicyResolver> logger)
    : IDynamicCorsPolicyResolver
{
    private readonly HttpContext _context = httpContextAccessor?.HttpContext;

    private string ApiBaseUrl
    {
        get
        {        
            var apiBaseUrl = setupInfo.WebApiBaseUrl;
            if (Uri.IsWellFormedUriString(apiBaseUrl, UriKind.Relative))
            {
                apiBaseUrl = linkUtility.GetFullAbsolutePath(apiBaseUrl);
            }
            return apiBaseUrl;
        }
    }

    public async Task<bool> ResolveForOrigin(string origin)
    {
        logger.DebugCheckOrigin(origin);

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

        if (!Guid.TryParse(subject, out var userId))
        {
            return new List<string>();
        }

        var claimIdClaim = token.Claims.Single(c => string.Equals(c.Type, "cid", StringComparison.OrdinalIgnoreCase));
        var clientId = Guid.Parse(claimIdClaim.Value);

        var tenantId = tenantManager.GetCurrentTenantId();
        var cookieValue = await cookieStorage.EncryptCookieAsync(tenantId, userId, 0);
        var cookieName = cookiesManager.GetAscCookiesName();

        var origins = memoryCache.Get<IEnumerable<string>>(clientId);

        if (origins == null)
        {
            using var httpClient = httpClientFactory.CreateClient();

            var requestUri = new Uri($"{ApiBaseUrl}clients/{clientId}");

            httpClient.DefaultRequestHeaders.Add("Cookie", $"{cookieName}={cookieValue}");

            var httpResponse = await httpClient.GetStringAsync(requestUri);

            var forecastNode = JsonNode.Parse(httpResponse)!;

            origins = forecastNode!["allowed_origins"]!.AsArray().Select(x => x.GetValue<string>());

            memoryCache.Set(clientId, origins, TimeSpan.FromMinutes(15));
        }

        logger.DebugGetOriginsFromOAuth2App(clientId, origins);

        return origins;
    }

}
