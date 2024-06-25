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

using ASC.Core.Notify.Socket;
using ASC.MessagingSystem.Data;

namespace ASC.Api.Core;

public class BaseWorkerStartup(IConfiguration configuration, IHostEnvironment hostEnvironment)
{
    protected IConfiguration Configuration { get; } = configuration;
    protected IHostEnvironment HostEnvironment { get; } = hostEnvironment;
    protected DIHelper DIHelper { get; } = new();

    public virtual async Task ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddCustomHealthCheck(Configuration);

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


        services.RegisterFeature();

        services.AddAutoMapper(GetAutoMapperProfileAssemblies());

        if (!HostEnvironment.IsDevelopment())
        {
            services.AddStartupTask<WarmupServicesStartupTask>().TryAddSingleton(services);
        }

        services.AddMemoryCache();
        
        var connectionMultiplexer = await services.GetRedisConnectionMultiplexerAsync(Configuration, GetType().Namespace);

        services.AddDistributedCache(connectionMultiplexer)
                .AddEventBus(Configuration)
                .AddDistributedTaskQueue()
                .AddCacheNotify(Configuration)
                .AddHttpClient()
                .AddDistributedLock(Configuration);


        DIHelper.Configure(services);
        DIHelper.Scan();
        
        services.AddSingleton(Channel.CreateUnbounded<NotifyRequest>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<NotifyRequest>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<NotifyRequest>>().Writer);
        services.AddHostedService<NotifySenderService>();
        
        services.AddSingleton(Channel.CreateUnbounded<SocketData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Writer);
        services.AddHostedService<SocketService>();
    }

    protected IEnumerable<Assembly> GetAutoMapperProfileAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.StartsWith("ASC."));
    }

    public virtual void Configure(IApplicationBuilder app)
    {
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