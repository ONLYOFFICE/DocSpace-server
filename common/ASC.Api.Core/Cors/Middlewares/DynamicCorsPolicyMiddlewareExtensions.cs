using ASC.Api.Core.Cors.Accessors;
using ASC.Api.Core.Cors.Resolvers;
using ASC.Api.Core.Cors.Services;

using Microsoft.AspNetCore.Cors.Infrastructure;

using Tweetinvi.Core.Models.Properties;

namespace ASC.Api.Core.Cors.Middlewares;

// Extension method used to add the middleware to the HTTP request pipeline.
public static class DynamicCorsPolicyMiddlewareExtensions
{
    public static IApplicationBuilder UseDynamicCorsMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<DynamicCorsPolicyMiddleware>();
    }

    public static IServiceCollection AddDynamicCors<TDynamicCorsPolicyResolver>(this IServiceCollection services, 
        Action<CorsOptions> setupAction)
        where TDynamicCorsPolicyResolver : class, IDynamicCorsPolicyResolver
    {
        services.AddCors(setupAction);

        services.AddTransient<IDynamicCorsPolicyService, DynamicCorsPolicyService>();
        services.AddTransient<ICorsPolicyAccessor, CorsPolicyAccessor>();
        services.AddTransient<IDynamicCorsPolicyResolver, TDynamicCorsPolicyResolver>();

        return services;
    }
}