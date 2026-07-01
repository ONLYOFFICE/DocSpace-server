// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

using ASC.Core.Common.Notify.Engine;

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
        if (configuration.GetSection("RabbitMQ").GetChildren().Any() &&
            string.IsNullOrEmpty(configuration["RabbitMQ:ClientProvidedName"]))
        {
            configuration["RabbitMQ:ClientProvidedName"] = Program.AppName;
        }
    }

    public async Task ConfigureServices(WebApplicationBuilder builder)
    {
        if (_configuration.GetValue<bool>("openTelemetry:enable"))
        {
            builder.ConfigureOpenTelemetry();
        }

        var services = builder.Services;
        services.AddCustomHealthCheck(_configuration);
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
        services.AddBaseDbContextPool<MessagesContext>();
        services.AddBaseDbContextPool<WebhooksDbContext>();
        services.AddBaseDbContextPool<FilesDbContext>();
        services.AddBaseDbContextPool<ApiKeysDbContext>();

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
                .AddCacheNotify(_configuration)
                .AddDistributedLock(_configuration)
                .AddHeartBeat(_configuration)
                .AddDistributedTaskQueue();

        services.RegisterFeature();
        services.RegisterQuotaFeature();

        if (_configuration.GetValue<bool>("openApi:enable"))
        {
            services.AddOpenApi(_configuration, "common");
        }
        if (!_hostEnvironment.IsDevelopment())
        {
            services.AddStartupTask<WarmupServicesStartupTask>()
                    .TryAddSingleton(services);
        }

        services.AddSingleton(Channel.CreateUnbounded<SocketData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Writer);
        services.AddScoped(_ => UrlEncoder.Default);

        services.AddBillingHttpClient();
        services.AddAccountingHttpClient(_configuration);

        services.AddApiSystemAuthServices();

        services.ConfigureNotificationServices();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseExceptionHandler();
        app.UseRouting();

        if (_configuration.GetValue<bool>("openApi:enable"))
        {
            app.UseOpenApi();
        }
        app.UseSynchronizationContextMiddleware();

        app.UseTenantMiddleware();

        if (!string.IsNullOrEmpty(_corsOrigin))
        {
            app.UseCors(CustomCorsPolicyName);
        }

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapCustomAsync();

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
