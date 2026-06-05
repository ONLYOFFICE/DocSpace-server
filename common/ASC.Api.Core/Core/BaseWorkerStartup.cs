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

namespace ASC.Api.Core;

public class BaseWorkerStartup(IConfiguration configuration)
{
    protected IConfiguration Configuration { get; } = configuration;
    private DIHelper DIHelper { get; } = new();

    private bool OpenTelemetryEnabled { get; } = configuration.GetValue<bool>("openTelemetry:enable");

    public virtual async Task ConfigureServices(WebApplicationBuilder builder)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AppContext.SetSwitch("System.Net.Security.UseManagedNtlm", true);
        }

        var services = builder.Services;
        services.AddHttpContextAccessor();
        services.AddCustomHealthCheck(Configuration);

        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddProblemDetails();

        if (OpenTelemetryEnabled)
        {
            builder.ConfigureOpenTelemetry();
        }
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
        services.AddBaseDbContextPool<MessagesContext>();
        services.AddBaseDbContextPool<WebhooksDbContext>();
        services.AddBaseDbContextPool<ApiKeysDbContext>();


        services.RegisterFeature();
        services.AddMemoryCache();

        var connectionMultiplexer = await services.GetRedisConnectionMultiplexerAsync(Configuration, GetType().Namespace);

        services.AddHybridCache(connectionMultiplexer)
                .AddMemoryCache(connectionMultiplexer)
                .AddEventBus(Configuration)
                .AddDistributedTaskQueue()
                .AddCacheNotify(Configuration)
                .AddHttpClient()
                .AddDistributedLock(Configuration)
                .AddHeartBeat(Configuration);

        DIHelper.Configure(services);
        DIHelper.Scan();

        services.ConfigureNotificationServices();

        services.AddSingleton(Channel.CreateUnbounded<SocketData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Writer);
        services.AddHostedService<SocketService>();
        services.AddSocketHttpClient(Configuration);
        services.AddTransient<DistributedTaskProgress>();

        services.AddBillingHttpClient();
        services.AddAccountingHttpClient();
        services.AddDocsCloudHttpClient();
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
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
