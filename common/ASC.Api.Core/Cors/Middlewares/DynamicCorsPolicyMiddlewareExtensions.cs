using ASC.Api.Core.Cors.Resolvers;
using ASC.Api.Core.Cors.Services;

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace ASC.Api.Core.Cors.Middlewares;

// Extension method used to add the middleware to the HTTP request pipeline.
public static class DynamicCorsPolicyMiddlewareExtensions
{
    public static IApplicationBuilder UseDynamicCorsMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<DynamicCorsPolicyMiddleware>();
    }

    public static IApplicationBuilder UseDynamicCorsMiddleware(this IApplicationBuilder app, string policyName)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<DynamicCorsPolicyMiddleware>(policyName);
    }


    public static IServiceCollection AddDynamicCors<TDynamicCorsPolicyResolver>(this IServiceCollection services, 
        Action<CorsOptions> setupAction)
        where TDynamicCorsPolicyResolver : class, IDynamicCorsPolicyResolver
    {
        services.AddCors(setupAction);

        services.AddTransient<IDynamicCorsPolicyService, DynamicCorsPolicyService>();
        services.AddTransient<IDynamicCorsPolicyResolver, TDynamicCorsPolicyResolver>();

        return services;
    }
}