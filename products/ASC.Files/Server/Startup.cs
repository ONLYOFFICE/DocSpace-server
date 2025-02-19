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

using ASC.Data.Storage;

namespace ASC.Files;

public class Startup : BaseStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
        WebhooksEnabled = true;

        if (String.IsNullOrEmpty(configuration["RabbitMQ:ClientProvidedName"]))
        {
            configuration["RabbitMQ:ClientProvidedName"] = Program.AppName;
        }
    }

    public override async Task ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddMemoryCache();

        await base.ConfigureServices(builder);

        services.Configure<DistributedTaskQueueFactoryOptions>(FileOperationsManagerHolder.CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME, x =>
        {
            x.MaxThreadsCount = 10;
        });
        
        services.AddBaseDbContextPool<FilesDbContext>();
        services.RegisterQuotaFeature();
        services.AddScoped<IWebItem, ProductEntryPoint>();
        services.AddDocumentServiceHttpClient(_configuration);

        services.AddStartupTask<CheckPdfStartupTask>()
           .TryAddSingleton(services);
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        base.Configure(app, env);
        if (_configuration.GetValue<bool>("openApi:enableUI"))
        {
            app.UseOpenApiUI();
        }
        app.MapWhen(
                context => context.Request.Path.ToString().EndsWith("filehandler.ashx", StringComparison.OrdinalIgnoreCase),
            appBranch =>
            {
                appBranch.UseFileHandler();
            });

        app.MapWhen(
                context => context.Request.Path.ToString().EndsWith("ChunkedUploader.ashx", StringComparison.OrdinalIgnoreCase),
            appBranch =>
            {
                appBranch.UseChunkedUploaderHandler();
            });

        app.MapWhen(
                context => context.Request.Path.ToString().EndsWith("DocuSignHandler.ashx", StringComparison.OrdinalIgnoreCase),
            appBranch =>
            {
                appBranch.UseDocuSignHandler();
            });

        app.UseEndpoints(endpoints =>
        {
            endpoints.InitializeHttpHandlers("files_template");
        });
    }
}
