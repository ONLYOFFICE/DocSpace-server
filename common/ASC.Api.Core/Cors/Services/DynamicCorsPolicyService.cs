using ASC.Api.Core.Cors.Resolvers;

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace ASC.Api.Core.Cors.Services;
public class DynamicCorsPolicyService : IDynamicCorsPolicyService
{
    private readonly CorsOptions _options;
    private readonly IDynamicCorsPolicyResolver _dynamicCorsPolicyResolver;

    public DynamicCorsPolicyService(IOptions<CorsOptions> options,
        IDynamicCorsPolicyResolver dynamicCorsPolicyResolver)
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
            IsOriginAllowed = await IsOriginAllowed(policy, origin),
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
            ? new[]
            {
                result.IsPreflightRequest
                    ? (string) context.Request.Headers[CorsConstants.AccessControlRequestMethod]
                    : context.Request.Method
            }
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

        for (var i = 0; i < headerValues.Count; i++)
        {
            target.Add(headerValues[i]);
        }
    }

    private async Task<bool> IsOriginAllowed(CorsPolicy policy, StringValues origin)
    {
        if (StringValues.IsNullOrEmpty(origin))
        {
            return false;
        }

        return policy.AllowAnyOrigin
               || policy.IsOriginAllowed(origin) 
               || await _dynamicCorsPolicyResolver.ResolveForOrigin(origin);
    }
}