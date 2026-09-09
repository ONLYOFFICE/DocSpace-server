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

        services.AddSingleton(Channel.CreateUnbounded<SocketData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<SocketData>>().Writer);
        services.AddScoped<AuthHandler>();

        services.AddBillingHttpClient(_configuration);
        services.AddAccountingHttpClient(_configuration);
        services.AddDocsCloudHttpClient(_configuration);

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
