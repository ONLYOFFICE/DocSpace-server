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

using ASC.MessagingSystem.Data;

namespace ASC.Web.Studio;

public class Startup : BaseStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
        if (String.IsNullOrEmpty(configuration["RabbitMQ:ClientProvidedName"]))
        {
            configuration["RabbitMQ:ClientProvidedName"] = Program.AppName;
        }
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        base.Configure(app, env);
        
        if (OpenApiEnabled && _configuration.GetValue<bool>("openApi:enableUI"))
        {
            var endpoints = new Dictionary<string,string>();
            _configuration.Bind("openApi:endpoints", endpoints);
            app.UseOpenApiUI(endpoints);
        }


        app.UseRouting();

        app.UseAuthentication();

        app.UseEndpoints(endpoints =>
        {
            endpoints.InitializeHttpHandlers();
        });

        app.MapWhen(
              context => context.Request.Path.ToString().EndsWith("ssologin.ashx"),
              appBranch =>
              {
                  appBranch.UseSsoHandler();
              });

        app.MapWhen(
            context => context.Request.Path.ToString().EndsWith("login.ashx"),
            appBranch =>
            {
                appBranch.UseLoginHandler();
            });
    }

    public override async Task ConfigureServices(IServiceCollection services)
    {
        await base.ConfigureServices(services);

        services.AddMemoryCache();
        services.AddBaseDbContextPool<FilesDbContext>();
        services.RegisterQuotaFeature();
        services.AddHttpClient();
        services.AddHostedService<WorkerService>();
        services.TryAddSingleton(new ConcurrentQueue<WebhookRequestIntegrationEvent>());

        services.AddSingleton(Channel.CreateUnbounded<EventData>());
        services.AddSingleton(svc => svc.GetRequiredService<Channel<EventData>>().Reader);
        services.AddSingleton(svc => svc.GetRequiredService<Channel<EventData>>().Writer);
        services.AddHostedService<MessageSenderService>();
        
        var lifeTime = TimeSpan.FromMinutes(5);

        Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policyHandler = (s, _) =>
        {
            var settings = s.GetRequiredService<Settings>();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(settings.RepeatCount ?? 5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        };

        services.AddHttpClient(WebhookSender.WEBHOOK)
        .SetHandlerLifetime(lifeTime)
        .AddPolicyHandler(policyHandler);

        services.AddHttpClient(WebhookSender.WEBHOOK_SKIP_SSL)
        .SetHandlerLifetime(lifeTime)
        .AddPolicyHandler(policyHandler)
        .ConfigurePrimaryHttpMessageHandler(_ =>
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
        });
    }
}
