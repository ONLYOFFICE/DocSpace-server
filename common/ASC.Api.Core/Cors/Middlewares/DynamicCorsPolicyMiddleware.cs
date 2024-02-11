using ASC.Api.Core.Cors.Accessors;
using ASC.Api.Core.Cors.Enums;
using ASC.Api.Core.Cors.Services;

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace ASC.Api.Core.Cors.Middlewares;
internal class DynamicCorsPolicyMiddleware
{
    private readonly Func<object, Task> _onResponseStartingDelegate = OnResponseStarting;
    private readonly RequestDelegate _next;
    private readonly CorsPolicy _policy;
    private IDynamicCorsPolicyService CorsService;

    public DynamicCorsPolicyMiddleware(
        RequestDelegate next,        
        ICorsPolicyAccessor corsPolicyAccessor)
    {
        if (corsPolicyAccessor == null)
        {
            throw new ArgumentNullException(nameof(corsPolicyAccessor));
        }
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _policy = corsPolicyAccessor.GetPolicy(CorsPoliciesEnums.DynamicCorsPolicyName);
    }


    public async Task Invoke(HttpContext context, ICorsPolicyProvider corsPolicyProvider, IDynamicCorsPolicyService corsService)
    {
        CorsService = corsService ?? throw new ArgumentNullException(nameof(corsService));

        if (!context.Request.Headers.ContainsKey(CorsConstants.Origin))
        {
            await _next(context);
        }
        else
        {
            await InvokeCore(context, corsPolicyProvider);
        }
    }

    private async Task InvokeCore(HttpContext context, ICorsPolicyProvider corsPolicyProvider)
    {
        var corsPolicy = _policy ?? await corsPolicyProvider.GetPolicyAsync(context, CorsPoliciesEnums.DynamicCorsPolicyName);
        if (corsPolicy == null)
        {
            await _next(context);
            return;
        }

        var corsResult = await CorsService.EvaluatePolicy(context, corsPolicy);
        if (corsResult.IsPreflightRequest)
        {
            CorsService.ApplyResult(corsResult, context.Response);

            // Since there is a policy which was identified,
            // always respond to preflight requests.
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }
        else
        {
            context.Response.OnStarting(_onResponseStartingDelegate, Tuple.Create(this, context, corsResult));
            await _next(context);
        }
    }



    private static Task OnResponseStarting(object state)
    {
        var (middleware, context, result) = (Tuple<DynamicCorsPolicyMiddleware, HttpContext, CorsResult>)state;
        try
        {
            middleware.CorsService.ApplyResult(result, context.Response);
        }
        catch (Exception)
        {
            //middleware.Logger.LogError(exception.Message);
        }
        return Task.CompletedTask;
    }
}