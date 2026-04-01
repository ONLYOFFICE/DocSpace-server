// (c) Copyright Ascensio System SIA 2009-2026
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

using ASC.Core.Common.Identity;
using ASC.Core.Security.Authentication;

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace ASC.Api.Core.Cors;

public class DynamicCorsPolicyProvider(
    IOptions<CorsOptions> options,
    IHttpClientFactory httpClientFactory,
    IdentityClient identityClient,
    CommonLinkUtility linkUtility,
    SetupInfo setupInfo,
    IMemoryCache memoryCache,
    ILogger<DynamicCorsPolicyProvider> logger) : ICorsPolicyProvider
{
    private readonly CorsOptions _options = options.Value;

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

    public async Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
    {
        ArgumentNullException.ThrowIfNull(context);

        var policy = _options.GetPolicy(policyName ?? _options.DefaultPolicyName);

        if (policy is null)
        {
            return policy;
        }

        var accessToken = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(accessToken) || accessToken.IndexOf("Bearer", 0, StringComparison.Ordinal) == -1)
        {
            return policy;
        }

        accessToken = accessToken.Trim();
        accessToken = accessToken["Bearer ".Length..].Trim();

        var jwtHandler = new JwtSecurityTokenHandler();

        if (!jwtHandler.CanReadToken(accessToken))
        {
            return policy;
        }

        var origins = await GetOriginsFromOAuth2AppAsync(accessToken);

        if (origins == null || !origins.Any())
        {
            return policy;
        }

        return BuildOAuthPolicy(policy, origins);
    }

    private static CorsPolicy BuildOAuthPolicy(CorsPolicy basePolicy, IEnumerable<string> oauthOrigins)
    {
        var builder = new CorsPolicyBuilder();

        builder.WithOrigins(oauthOrigins.ToArray())
            .SetIsOriginAllowedToAllowWildcardSubdomains();

        if (basePolicy.AllowAnyHeader)
        {
            builder.AllowAnyHeader();
        }
        else if (basePolicy.Headers.Count > 0)
        {
            builder.WithHeaders(basePolicy.Headers.ToArray());
        }

        if (basePolicy.AllowAnyMethod)
        {
            builder.AllowAnyMethod();
        }
        else if (basePolicy.Methods.Count > 0)
        {
            builder.WithMethods(basePolicy.Methods.ToArray());
        }

        if (basePolicy.ExposedHeaders.Count > 0)
        {
            builder.WithExposedHeaders(basePolicy.ExposedHeaders.ToArray());
        }

        if (basePolicy.SupportsCredentials)
        {
            builder.AllowCredentials();
        }

        if (basePolicy.PreflightMaxAge.HasValue)
        {
            builder.SetPreflightMaxAge(basePolicy.PreflightMaxAge.Value);
        }

        return builder.Build();
    }

    private async Task<IEnumerable<string>> GetOriginsFromOAuth2AppAsync(string accessToken)
    {
        var token = new JwtSecurityToken(accessToken);

        var subject = token.Subject;

        if (!Guid.TryParse(subject, out var userId))
        {
            return [];
        }

        var cidClaim = token.Claims.SingleOrDefault(c => string.Equals(c.Type, "cid", StringComparison.OrdinalIgnoreCase));

        if (cidClaim is null || !Guid.TryParse(cidClaim.Value, out var clientId))
        {
            return [];
        }

        var origins = memoryCache.Get<IEnumerable<string>>(clientId);

        if (origins is null)
        {
            using var httpClient = httpClientFactory.CreateClient();

            var requestUri = new Uri($"{ApiBaseUrl}clients/{clientId}");

            var jwtToken = await identityClient.GenerateJwtTokenAsync(null, userId);

            httpClient.DefaultRequestHeaders.Add("x-signature", $"{jwtToken}");

            var httpResponse = await httpClient.GetStringAsync(requestUri);

            var responseNode = JsonNode.Parse(httpResponse)!;

            origins = responseNode["allowed_origins"]?.AsArray().Select(x => x.GetValue<string>()).ToList() ?? [];

            memoryCache.Set(clientId, origins, TimeSpan.FromMinutes(15));
        }

        logger.DebugGetOriginsFromOAuth2App(clientId, origins);

        return origins;
    }
}
