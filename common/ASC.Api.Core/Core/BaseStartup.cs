﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using System.Diagnostics;
using ASC.Api.Core.Cors;
using ASC.Api.Core.Cors.Enums;
using ASC.Api.Core.Cors.Middlewares;
using ASC.Common.Mapping;
using ASC.Core.Notify.Socket;
using ASC.MessagingSystem;
using Flurl.Util;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace ASC.Api.Core;

public abstract class BaseStartup
{
    private const string BasicAuthScheme = "Basic";
    private const string MultiAuthSchemes = "MultiAuthSchemes";

    protected readonly IConfiguration _configuration;
    private readonly string _corsOrigin;
    private static readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    protected bool AddAndUseSession { get; }
    protected DIHelper DIHelper { get; }
    protected bool WebhooksEnabled { get; init; }

    protected bool OpenApiEnabled { get; init; }

    private bool OpenTelemetryEnabled { get; }

    protected BaseStartup(IConfiguration configuration)
    {
        _configuration = configuration;

        _corsOrigin = _configuration["core:cors"];

        DIHelper = new DIHelper();
        OpenApiEnabled = _configuration.GetValue<bool>("openApi:enable");
        OpenTelemetryEnabled = _configuration.GetValue<bool>("openTelemetry:enable");
    }

    public virtual async Task ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AppContext.SetSwitch("System.Net.Security.UseManagedNtlm", true);
        }
        
        services.AddCustomHealthCheck(_configuration);
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.AddHttpClient();
        services.AddHttpClient("customHttpClient", _ => { }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddProblemDetails();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = null;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            var knownProxies = _configuration.GetSection("core:hosting:forwardedHeadersOptions:knownProxies").Get<List<String>>();
            var knownNetworks = _configuration.GetSection("core:hosting:forwardedHeadersOptions:knownNetworks").Get<List<String>>();
            var allowedHosts = _configuration.GetSection("core:hosting:forwardedHeadersOptions:allowedHosts").Get<List<String>>();

            if (allowedHosts is { Count: > 0 })
            {
                options.ForwardedHeaders |= ForwardedHeaders.XForwardedHost;
                options.AllowedHosts = allowedHosts;
            }

            if (knownProxies is { Count: > 0 })
            {
                foreach (var knownProxy in knownProxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(knownProxy));
                }
            }


            if (knownNetworks is { Count: > 0 })
            {
                foreach (var knownNetwork in knownNetworks)
                {
                    var prefix = IPAddress.Parse(knownNetwork.Split("/")[0]);
                    var prefixLength = Convert.ToInt32(knownNetwork.Split("/")[1]);

                    options.KnownNetworks.Add(new IPNetwork(prefix, prefixLength));
                }
            }
        });

        var connectionMultiplexer = await services.GetRedisConnectionMultiplexerAsync(_configuration, GetType().Namespace);

        services.AddRateLimiter(options =>
        {
            bool EnableNoLimiter(IPAddress address)
            {
                var knownNetworks = _configuration.GetSection("core:hosting:rateLimiterOptions:knownNetworks").Get<List<String>>();
                var knownIPAddresses = _configuration.GetSection("core:hosting:rateLimiterOptions:knownIPAddresses").Get<List<String>>();

                if (knownIPAddresses is { Count: > 0 })
                {
                    foreach (var knownIPAddress in knownIPAddresses)
                    {
                        if (IPAddress.Parse(knownIPAddress).Equals(address))
                        {
                            return true;
                        }
                    }
                }

                if (knownNetworks is { Count: > 0 })
                {
                    foreach (var knownNetwork in knownNetworks)
                    {
                        var prefix = IPAddress.Parse(knownNetwork.Split("/")[0]);
                        var prefixLength = Convert.ToInt32(knownNetwork.Split("/")[1]);
                        var ipNetwork = new IPNetwork(prefix, prefixLength);

                        if (ipNetwork.Contains(address))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }


            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var userId = httpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid)?.Value ??
                                 httpContext?.Connection.RemoteIpAddress.ToInvariantString();

                    var remoteIpAddress = httpContext?.Connection.RemoteIpAddress;

                    if (EnableNoLimiter(remoteIpAddress))
                    {
                        return RateLimitPartition.GetNoLimiter("no_limiter");
                    }

                    userId ??= remoteIpAddress.ToInvariantString();

                    var permitLimit = 1500;

                    var partitionKey = $"sw_{userId}";

                    return RedisRateLimitPartition.GetSlidingWindowRateLimiter(partitionKey, _ => new RedisSlidingWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1), ConnectionMultiplexerFactory = () => connectionMultiplexer });
                }),
                PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var userId = httpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid)?.Value;
                    string partitionKey;
                    int permitLimit;

                    var remoteIpAddress = httpContext?.Connection.RemoteIpAddress;

                    if (EnableNoLimiter(remoteIpAddress))
                    {
                        return RateLimitPartition.GetNoLimiter("no_limiter");
                    }

                    userId ??= remoteIpAddress.ToInvariantString();

                    if (String.Compare(httpContext?.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        permitLimit = 50;
                        partitionKey = $"cr_read_{userId}";
                    }
                    else
                    {
                        permitLimit = _configuration.GetSection("core:hosting:rateLimiterOptions:defaultConcurrencyWriteRequests").Get<int>();

                        if (permitLimit == 0)
                        {
                            permitLimit = 15;
                        }

                        partitionKey = $"cr_write_{userId}";
                    }

                    return RedisRateLimitPartition.GetConcurrencyRateLimiter(partitionKey, _ => new RedisConcurrencyRateLimiterOptions { PermitLimit = permitLimit, QueueLimit = 0, ConnectionMultiplexerFactory = () => connectionMultiplexer });
                }),
                PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    {
                        var userId = httpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid)?.Value ??
                                     httpContext?.Connection.RemoteIpAddress.ToInvariantString();

                        var remoteIpAddress = httpContext?.Connection.RemoteIpAddress;

                        if (EnableNoLimiter(remoteIpAddress))
                        {
                            return RateLimitPartition.GetNoLimiter("no_limiter");
                        }

                        userId ??= remoteIpAddress.ToInvariantString();

                        var partitionKey = $"fw_post_put_{userId}";
                        var permitLimit = 10000;

                        if (!(String.Compare(httpContext?.Request.Method, "POST", StringComparison.OrdinalIgnoreCase) == 0 ||
                              String.Compare(httpContext?.Request.Method, "PUT", StringComparison.OrdinalIgnoreCase) == 0))
                        {
                            return RateLimitPartition.GetNoLimiter("no_limiter");
                        }

                        return RedisRateLimitPartition.GetFixedWindowRateLimiter(partitionKey, _ => new RedisFixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromDays(1), ConnectionMultiplexerFactory = () => connectionMultiplexer });
                    }
                ));

            options.AddPolicy(RateLimiterPolicy.SensitiveApi, httpContext =>
            {
                var userId = httpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid)?.Value ??
                             httpContext?.Connection.RemoteIpAddress.ToInvariantString();

                var permitLimit = 5;
                var path = httpContext?.Request.Path.ToString();
                var partitionKey = $"{RateLimiterPolicy.SensitiveApi}_{userId}|{path}";
                var remoteIpAddress = httpContext?.Connection.RemoteIpAddress;

                if (EnableNoLimiter(remoteIpAddress))
                {
                    return RateLimitPartition.GetNoLimiter("no_limiter");
                }

                return RedisRateLimitPartition.GetSlidingWindowRateLimiter(partitionKey, _ => new RedisSlidingWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(15), ConnectionMultiplexerFactory = () => connectionMultiplexer });
            });

            options.AddPolicy(RateLimiterPolicy.EmailInvitationApi, httpContext =>
            {
                if (!int.TryParse(_configuration["core:hosting:rateLimiterOptions:maxEmailInvitationsPerDay"], out var invitationLimitPerDay))
                {
                    return RateLimitPartition.GetNoLimiter("no_limiter");
                }

                var remoteIpAddress = httpContext?.Connection.RemoteIpAddress;

                if (EnableNoLimiter(remoteIpAddress))
                {
                    return RateLimitPartition.GetNoLimiter("no_limiter");
                }

                var tenantManager = httpContext.RequestServices.GetRequiredService<TenantManager>();
                var tenant = tenantManager.GetCurrentTenant(false);

                if (tenant == null)
                {
                    return RateLimitPartition.GetNoLimiter("no_limiter");
                }

                var invitationsCount = 0;

                if (httpContext.Request.ContentLength > 0)
                {
                    if (!httpContext.Request.Body.CanSeek)
                    {
                        httpContext.Request.EnableBuffering();
                    }

                    httpContext.Request.Body.Position = 0;

                    var json = new StreamReader(httpContext.Request.Body).ReadToEndAsync().Result;

                    var userInvitationsDto = JsonSerializer.Deserialize<EmailInvitationsDto>(json, _serializerOptions);
                    invitationsCount = userInvitationsDto.Invitations.Count(x => !string.IsNullOrEmpty(x.Email));

                    httpContext.Request.Body.Position = 0;
                }

                if (invitationsCount == 0)
                {
                    return RateLimitPartition.GetNoLimiter("no_limiter");
                }

                var partitionKey = $"{RateLimiterPolicy.EmailInvitationApi}_{tenant.Id}";

                RedisFixedWindowRateLimiterOptions OptionFactory() => new() { PermitLimit = invitationLimitPerDay, Window = TimeSpan.FromDays(1), ConnectionMultiplexerFactory = () => connectionMultiplexer };

                RateLimiter LimitterFactory(string key) => new LooppedRedisFixedWindowRateLimiter<string>(key, OptionFactory(), invitationsCount);

                return RateLimitPartition.Get(partitionKey, LimitterFactory);
            });

            options.OnRejected = (context, ct) => RateLimitMetadata.OnRejected(context.HttpContext, context.Lease, ct);
        });

        services.AddSingleton<MessageSettings>();//warmup
        services.AddSingleton<EFLoggerFactory>();

        services.AddBaseDbContextPool<AccountLinkContext>()
            .AddBaseDbContextPool<CoreDbContext>()
            .AddBaseDbContextPool<TenantDbContext>()
            .AddBaseDbContextPool<UserDbContext>()
            .AddBaseDbContextPool<TelegramDbContext>()
            .AddBaseDbContextPool<FirebaseDbContext>()
            .AddBaseDbContextPool<CustomDbContext>()
            .AddBaseDbContextPool<UrlShortenerDbContext>()
            .AddBaseDbContextPool<WebstudioDbContext>()
            .AddBaseDbContextPool<InstanceRegistrationContext>()
            .AddBaseDbContextPool<IntegrationEventLogContext>()
            .AddBaseDbContextPool<MessagesContext>()
            .AddBaseDbContextPool<WebhooksDbContext>();

        if (AddAndUseSession)
        {
            services.AddSession();
        }

        DIHelper.Configure(services);

        Action<JsonOptions> jsonOptions = options =>
        {
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        };

        services.AddControllers().AddJsonOptions(jsonOptions);

        services.AddSingleton(jsonOptions);
        
        DIHelper.Scan();

        if (!string.IsNullOrEmpty(_corsOrigin))
        {
            services.AddDynamicCors<DynamicCorsPolicyResolver>(options =>
            {
                options.AddPolicy(name: CorsPoliciesEnums.DynamicCorsPolicyName,
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

                options.AddPolicy(name: CorsPoliciesEnums.AllowAllCorsPolicyName,
                                  policy =>
                                  {
                                      policy.WithOrigins("*")
                                            .SetIsOriginAllowedToAllowWildcardSubdomains()
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                                  });
            });
        }


        services.AddDistributedCache(connectionMultiplexer)
            .AddEventBus(_configuration)
            .AddDistributedTaskQueue()
            .AddCacheNotify(_configuration)
            .AddDistributedLock(_configuration);

        services.RegisterFeature();

        services.AddOptions();

        var mvcBuilder = services.AddMvcCore(config =>
        {
            config.Conventions.Add(new ControllerNameAttributeConvention());

            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

            config.Filters.Add(new AuthorizeFilter(policy));
            config.Filters.Add(new TypeFilterAttribute(typeof(TenantStatusFilter)));
            config.Filters.Add(new TypeFilterAttribute(typeof(PaymentFilter)));
            config.Filters.Add(new TypeFilterAttribute(typeof(IpSecurityFilter)));
            config.Filters.Add(new TypeFilterAttribute(typeof(ProductSecurityFilter)));
            config.Filters.Add(new CustomResponseFilterAttribute());
            config.Filters.Add(new TypeFilterAttribute(typeof(WebhooksGlobalFilterAttribute)));
        });

        if (OpenApiEnabled)
        {
            mvcBuilder.AddApiExplorer();
            services.AddOpenApi(_configuration);
        }
        if (OpenTelemetryEnabled)
        {
            builder.ConfigureOpenTelemetry();
        }
        services.AddScoped<CookieAuthHandler>();
        services.AddScoped<BasicAuthHandler>();
        services.AddScoped<ConfirmAuthHandler>();
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = MultiAuthSchemes;
                options.DefaultChallengeScheme = MultiAuthSchemes;
            })
            .AddScheme<AuthenticationSchemeOptions, CookieAuthHandler>(CookieAuthenticationDefaults.AuthenticationScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, ConfirmAuthHandler>("confirm", _ => { })
            .AddPolicyScheme(MultiAuthSchemes, JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    string authorizationHeader = context.Request.Headers[HeaderNames.Authorization];

                    if (string.IsNullOrEmpty(authorizationHeader))
                    {
                        return CookieAuthenticationDefaults.AuthenticationScheme;
                    }

                    if (authorizationHeader.StartsWith("Basic "))
                    {
                        return BasicAuthScheme;
                    }

                    if (authorizationHeader.StartsWith("Bearer "))
                    {
                        var token = authorizationHeader["Bearer ".Length..].Trim();
                        var jwtHandler = new JwtSecurityTokenHandler();

                        if (jwtHandler.CanReadToken(token))
                        {
                            return JwtBearerDefaults.AuthenticationScheme;
                        }
                    }

                    return CookieAuthenticationDefaults.AuthenticationScheme;
                };
            });

        services.AddJwtBearerAuthentication();

        services.AddAutoMapper(GetAutoMapperProfileAssemblies());

        services.AddBillingHttpClient();

        services.AddSingleton(Channel.CreateUnbounded<NotifyRequest>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<NotifyRequest>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<NotifyRequest>>().Writer);
        services.AddHostedService<NotifySenderService>();

        services.AddSingleton(Channel.CreateUnbounded<SocketData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Writer);
        services.AddHostedService<SocketService>();
        
        services.Configure<DistributedTaskQueueFactoryOptions>(UserPhotoManager.CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME, options =>
        {
            options.MaxThreadsCount = 2;
        });

        services
            .AddStartupTask<WarmupServicesStartupTask>()
            .AddStartupTask<WarmupProtobufStartupTask>()
            .AddStartupTask<WarmupBaseDbContextStartupTask>()
            .AddStartupTask<WarmupMappingStartupTask>()
            .TryAddSingleton(services);
        
        services.AddTransient<DistributedTaskProgress>();
    }

    public static IEnumerable<Assembly> GetAutoMapperProfileAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.StartsWith("ASC."));
    }

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseRouting();

        app.Use(next => async context =>
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            context.Response.OnStarting(() =>
            {
                stopWatch.Stop();

                var headerValue = $"aspnetcore-request-time;dur={stopWatch.ElapsedMilliseconds}ms";

                context.Response.Headers.Append("Server-Timing", headerValue);

                return Task.CompletedTask;
            });

            await next(context);
        });

        if (!string.IsNullOrEmpty(_corsOrigin))
        {
            app.UseDynamicCorsMiddleware(CorsPoliciesEnums.DynamicCorsPolicyName);
        }

        if (AddAndUseSession)
        {
            app.UseSession();
        }

        app.UseSynchronizationContextMiddleware();

        app.UseTenantMiddleware();
        
        app.UseAuthentication();

        // TODO: if some client requests very slow, this line will need to remove
        bool.TryParse(_configuration["core:hosting:rateLimiterOptions:enable"], out var enableRateLimiter);

        if (enableRateLimiter)
        {
            app.UseRateLimiter();
        }

        app.UseAuthorization();

        app.UseCultureMiddleware();

        app.UseLoggerMiddleware();


        if (OpenApiEnabled)
        {
            app.UseOpenApi();
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapCustomAsync(WebhooksEnabled, app.ApplicationServices).Wait();

            endpoints.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true, ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse }).ShortCircuit();

            endpoints.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = r => r.Name.Contains("services") });

            endpoints.MapHealthChecks("/liveness", new HealthCheckOptions { Predicate = r => r.Name.Contains("self") });
        });

        app.Map("/switch", appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                CustomHealthCheck.Running = !CustomHealthCheck.Running;
                await context.Response.WriteAsync($"{Environment.MachineName} running {CustomHealthCheck.Running}");
            });
        });
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        builder.Register(_configuration);
    }
}