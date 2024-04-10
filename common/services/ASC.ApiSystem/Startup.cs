// (c) Copyright Ascensio System SIA 2009-2024
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

using System.Threading.Channels;

using ASC.Core.Notify.Socket;

namespace ASC.ApiSystem;

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
        if (String.IsNullOrEmpty(configuration["RabbitMQ:ClientProvidedName"]))
        {
            configuration["RabbitMQ:ClientProvidedName"] = Program.AppName;
        }
    }

    public async Task ConfigureServices(IServiceCollection services)
    {
        services.AddCustomHealthCheck(_configuration);
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddExceptionHandler<Classes.CustomExceptionHandler>();
        services.AddProblemDetails();

        services.AddSingleton<EFLoggerFactory>();
        services.AddBaseDbContextPool<AccountLinkContext>();
        services.AddBaseDbContextPool<CoreDbContext>();
        services.AddBaseDbContextPool<TenantDbContext>();
        services.AddBaseDbContextPool<UserDbContext>();
        services.AddBaseDbContextPool<TelegramDbContext>();
        services.AddBaseDbContextPool<FirebaseDbContext>();
        services.AddBaseDbContextPool<CustomDbContext>();
        services.AddBaseDbContextPool<UrlShortenerDbContext>();
        services.AddBaseDbContextPool<WebstudioDbContext>();
        services.AddBaseDbContextPool<InstanceRegistrationContext>();
        services.AddBaseDbContextPool<IntegrationEventLogContext>();
        services.AddBaseDbContextPool<FeedDbContext>();
        services.AddBaseDbContextPool<MessagesContext>();
        services.AddBaseDbContextPool<WebhooksDbContext>();
        services.AddBaseDbContextPool<FilesDbContext>();

        services.AddSession();

        _diHelper.Configure(services);

        Action<JsonOptions> jsonOptions = options =>
        {
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        };

        services.AddControllers()
            .AddXmlSerializerFormatters()
            .AddJsonOptions(jsonOptions);

        services.AddSingleton(jsonOptions);

        _diHelper.AddControllers();
        _diHelper.TryAdd<IpSecurityFilter>();
        _diHelper.TryAdd<PaymentFilter>();
        _diHelper.TryAdd<ProductSecurityFilter>();
        _diHelper.TryAdd<TenantStatusFilter>();
        _diHelper.TryAdd<ConfirmAuthHandler>();
        _diHelper.TryAdd<BasicAuthHandler>();
        _diHelper.TryAdd<CookieAuthHandler>();
        _diHelper.TryAdd<WebhooksGlobalFilterAttribute>();
        _diHelper.TryAdd<FilesSpaceUsageStatManager>();
        _diHelper.TryAdd<ApiSystemAuthHandlerHelper>();

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

        services.AddDistributedCache(connectionMultiplexer)
                .AddEventBus(_configuration)
                .AddDistributedTaskQueue()
                .AddCacheNotify(_configuration)
                .AddDistributedLock(_configuration);

        services.RegisterFeature();

        services.AddScoped<ITenantQuotaFeatureStat<CountRoomFeature, int>, CountRoomCheckerStatistic>();
        services.AddScoped<CountRoomCheckerStatistic>();

        _diHelper.TryAdd(typeof(IWebhookPublisher), typeof(WebhookPublisher));

        services.AddAutoMapper(BaseStartup.GetAutoMapperProfileAssemblies());

        if (!_hostEnvironment.IsDevelopment())
        {
            services.AddStartupTask<WarmupServicesStartupTask>()
                    .TryAddSingleton(services);
        }
        
        services.AddSingleton(Channel.CreateUnbounded<SocketData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Writer);
        _diHelper.TryAdd<SocketService>();
        
        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, AuthHandler>("auth:allowskip:default", _ => { })
            .AddScheme<AuthenticationSchemeOptions, AuthHandler>("auth:allowskip:registerportal", _ => { })
            .AddScheme<AuthenticationSchemeOptions, ApiSystemAuthHandler>("auth:portal", _ => { })
            .AddScheme<AuthenticationSchemeOptions, ApiSystemBasicAuthHandler>("auth:portalbasic", _ => { });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseExceptionHandler();

        app.UseRouting();

        if (!string.IsNullOrEmpty(_corsOrigin))
        { 
            app.UseCors(CustomCorsPolicyName);
        }

        app.UseSynchronizationContextMiddleware();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapCustomAsync().Wait();

            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).ShortCircuit();

            endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
            {
                Predicate = r => r.Name.Contains("self")
            });
        });
    }
}