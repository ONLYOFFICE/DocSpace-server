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

using ASC.Core.Billing;

namespace ASC.Site.Core;

public class Startup
{
    private const string CustomCorsPolicyName = "Basic";
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly DIHelper _diHelper;
    private readonly string _corsOrigin;

    public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _diHelper = new DIHelper();
        _corsOrigin = _configuration["core:cors"];
    }

    public async Task ConfigureServices(WebApplicationBuilder builder)
    {        
        var services = builder.Services;

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddProblemDetails();

        services.AddSingleton<EFLoggerFactory>();
        services.AddBaseDbContextPool<AccountLinkContext>();
        services.AddBaseDbContextPool<CoreDbContext>();
        services.AddBaseDbContextPool<TenantDbContext>();
        services.AddBaseDbContextPool<UserDbContext>();
        services.AddBaseDbContextPool<CustomDbContext>();
        services.AddBaseDbContextPool<WebstudioDbContext>();


        services.AddBaseDbContextPool<EuRegionDbContext>(region: "eu", nameConnectionString: "default");
        services.AddBaseDbContextPool<UsRegionDbContext>(region: "us", nameConnectionString: "default");


        services.AddSession();

        _diHelper.Configure(services);
        _diHelper.Scan();

        Action<JsonOptions> jsonOptions = options =>
        {
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        };

        services.AddControllers()
            .AddXmlSerializerFormatters()
            .AddJsonOptions(jsonOptions);

        services.AddSingleton(jsonOptions);

        if (!string.IsNullOrEmpty(_corsOrigin))
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: CustomCorsPolicyName,
                                  policy =>
                                  {
                                      policy.WithOrigins(_corsOrigin)
                                            .SetIsOriginAllowedToAllowWildcardSubdomains()
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();

                                      if (_corsOrigin != "*")
                                      {
                                          policy.AllowCredentials();
                                      }
                                  });

            });
        }

        var connectionMultiplexer = await services.GetRedisConnectionMultiplexerAsync(_configuration, GetType().Namespace);

        services.AddHybridCache(connectionMultiplexer)
                .AddMemoryCache(connectionMultiplexer)
                .AddEventBus(_configuration)
                .AddDistributedLock(_configuration)
                .AddCacheNotify(_configuration);

        services.RegisterFeature();
        services.RegisterQuotaFeature();

        services.AddAutoMapper(BaseStartup.GetAutoMapperProfileAssemblies());

        services.AddSingleton(Channel.CreateUnbounded<SocketData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Writer);
        services.AddScoped<AuthHandler>();

        services.AddBillingHttpClient();
        services.AddAccountingHttpClient();

        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, AuthHandler>("auth:allowskip:default", _ => { });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseExceptionHandler();
        app.UseRouting();

        app.UseSynchronizationContextMiddleware();

        //app.UseTenantMiddleware();

        if (!string.IsNullOrEmpty(_corsOrigin))
        { 
            app.UseCors(CustomCorsPolicyName);
        }
        
        app.UseAuthentication();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapCustomAsync();
        });

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("/login"),
            appBranch =>
            {
                appBranch.UseLoginHandler();
            });
    }
}