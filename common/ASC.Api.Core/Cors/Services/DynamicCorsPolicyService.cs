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

using ASC.Api.Core.Cors.Resolvers;

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace ASC.Api.Core.Cors.Services;
public class DynamicCorsPolicyService : IDynamicCorsPolicyService
{
    private readonly CorsOptions _options;
    private readonly IDynamicCorsPolicyResolver _dynamicCorsPolicyResolver;

    public DynamicCorsPolicyService(IOptions<CorsOptions> options, IDynamicCorsPolicyResolver dynamicCorsPolicyResolver)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _dynamicCorsPolicyResolver = dynamicCorsPolicyResolver;
    }

    public async Task<CorsResult> EvaluatePolicy(HttpContext context, string policyName)
    {
        ArgumentNullException.ThrowIfNull(context);

        var policy = _options.GetPolicy(policyName);
        return await EvaluatePolicy(context, policy);
    }

    public async Task<CorsResult> EvaluatePolicy(HttpContext context, CorsPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);

        var origin = context.Request.Headers[CorsConstants.Origin];
        var requestHeaders = context.Request.Headers;

        var isOptionsRequest = string.Equals(context.Request.Method, CorsConstants.PreflightHttpMethod,
            StringComparison.OrdinalIgnoreCase);
        var isPreflightRequest =
            isOptionsRequest && requestHeaders.ContainsKey(CorsConstants.AccessControlRequestMethod);

        var corsResult = new CorsResult
        {
            IsPreflightRequest = isPreflightRequest,
            IsOriginAllowed = await IsOriginAllowed(policy, origin)
        };

        if (isPreflightRequest)
        {
            EvaluatePreflightRequest(context, policy, corsResult);
        }
        else
        {
            EvaluateRequest(context, policy, corsResult);
        }

        return corsResult;
    }

    private static void PopulateResult(HttpContext context, CorsPolicy policy, CorsResult result)
    {
        if (policy.AllowAnyOrigin)
        {
            result.AllowedOrigin = CorsConstants.AnyOrigin;
            result.VaryByOrigin = policy.SupportsCredentials;
        }
        else
        {
            var origin = context.Request.Headers[CorsConstants.Origin];
            result.AllowedOrigin = origin;
            result.VaryByOrigin = policy.Origins.Count > 1;
        }

        result.SupportsCredentials = policy.SupportsCredentials;
        result.PreflightMaxAge = policy.PreflightMaxAge;

        AddHeaderValues(result.AllowedExposedHeaders, policy.ExposedHeaders);

        var allowedMethods = policy.AllowAnyMethod
            ?
            [
                result.IsPreflightRequest
                    ? context.Request.Headers[CorsConstants.AccessControlRequestMethod]
                    : context.Request.Method
            ]
            : policy.Methods;
        AddHeaderValues(result.AllowedMethods, allowedMethods);

        var allowedHeaders = policy.AllowAnyHeader
            ? context.Request.Headers.GetCommaSeparatedValues(CorsConstants.AccessControlRequestHeaders)
            : policy.Headers;
        AddHeaderValues(result.AllowedHeaders, allowedHeaders);
    }

    public virtual void EvaluateRequest(HttpContext context, CorsPolicy policy, CorsResult result)
    {
        PopulateResult(context, policy, result);
    }

    public virtual void EvaluatePreflightRequest(HttpContext context, CorsPolicy policy, CorsResult result)
    {
        PopulateResult(context, policy, result);
    }

    public virtual void ApplyResult(CorsResult result, HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(response);

        if (!result.IsOriginAllowed)
        {
            // In case a server does not wish to participate in the CORS protocol, its HTTP response to the
            // CORS or CORS-preflight request must not include any of the above headers.
            return;
        }

        var headers = response.Headers;
        headers.AccessControlAllowOrigin = result.AllowedOrigin;

        if (result.SupportsCredentials)
        {
            headers.AccessControlAllowCredentials = "true";
        }

        if (result.IsPreflightRequest)
        {
            // An HTTP response to a CORS-preflight request can include the following headers:
            // `Access-Control-Allow-Methods`, `Access-Control-Allow-Headers`, `Access-Control-Max-Age`
            if (result.AllowedHeaders.Count > 0)
            {
                headers.SetCommaSeparatedValues(CorsConstants.AccessControlAllowHeaders, result.AllowedHeaders.ToArray());
            }

            if (result.AllowedMethods.Count > 0)
            {
                headers.SetCommaSeparatedValues(CorsConstants.AccessControlAllowMethods, result.AllowedMethods.ToArray());
            }

            if (result.PreflightMaxAge.HasValue)
            {
                headers.AccessControlMaxAge = result.PreflightMaxAge.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }
        }
        else
        {
            // An HTTP response to a CORS request that is not a CORS-preflight request can also include the following header:
            // `Access-Control-Expose-Headers`
            if (result.AllowedExposedHeaders.Count > 0)
            {
                headers.SetCommaSeparatedValues(CorsConstants.AccessControlExposeHeaders, result.AllowedExposedHeaders.ToArray());
            }
        }

        if (result.VaryByOrigin)
        {
            headers.Append(HeaderNames.Vary, "Origin");
        }
    }

    private static void AddHeaderValues(IList<string> target, IList<string> headerValues)
    {
        if (headerValues == null)
        {
            return;
        }

        foreach (var t in headerValues)
        {
            target.Add(t);
        }
    }

    private async Task<bool> IsOriginAllowed(CorsPolicy policy, StringValues origin)
    {
        if (StringValues.IsNullOrEmpty(origin))
        {
            return false;
        }

        return await _dynamicCorsPolicyResolver.ResolveForOrigin(policy, origin);
    }
}