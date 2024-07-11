using Microsoft.AspNetCore.Cors.Infrastructure;

namespace ASC.Api.Core.Cors.Services;
internal interface IDynamicCorsPolicyService
{
    void ApplyResult(CorsResult result, HttpResponse response);
    Task<CorsResult> EvaluatePolicy(HttpContext context, CorsPolicy policy);
}