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

using NLog;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
};

var builder = WebApplication.CreateBuilder(options);

builder.Configuration.AddDefaultConfiguration(builder.Environment)
                     .AddFilesServiceConfiguration(builder.Environment)
                     .AddEnvironmentVariables()
                     .AddCommandLine(args);

var logger = LogManager.Setup()
                            .SetupExtensions(s =>
                            {
                                s.RegisterLayoutRenderer("application-context", _ => AppName);
                            })
                            .LoadConfiguration(builder.Configuration, builder.Environment)
                            .GetLogger(typeof(Startup).Namespace);

try
{
    logger.Info("Configuring web host ({applicationContext})...", AppName);

    builder.Host.ConfigureDefault();
    builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
    {
        containerBuilder.Register(context.Configuration, true, "search.json", "feed.json");
    });

    var startup = new Startup(builder.Configuration, builder.Environment);

    await startup.ConfigureServices(builder.Services);

    var app = builder.Build();

    startup.Configure(app);

    var eventBus = ((IApplicationBuilder)app).ApplicationServices.GetRequiredService<IEventBus>();

    eventBus.Subscribe<ThumbnailRequestedIntegrationEvent, ThumbnailRequestedIntegrationEventHandler>();
    eventBus.Subscribe<RoomIndexExportIntegrationEvent, RoomIndexExportIntegrationEventHandler>();
    eventBus.Subscribe<DeleteIntegrationEvent, DeleteIntegrationEventHandler>();
    eventBus.Subscribe<MoveOrCopyIntegrationEvent, MoveOrCopyIntegrationEventHandler>();
    eventBus.Subscribe<BulkDownloadIntegrationEvent, BulkDownloadIntegrationEventHandler>();
    eventBus.Subscribe<MarkAsReadIntegrationEvent, MarkAsReadIntegrationEventHandler>();

    logger.Info("Starting web host ({applicationContext})...", AppName);
    await app.RunWithTasksAsync();
}
catch (Exception ex)
{
    logger?.Error(ex, "Program terminated unexpectedly ({applicationContext})!", AppName);

    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}

public partial class Program
{
    public static readonly string Namespace = typeof(Startup).Namespace;
    public static readonly string AppName = Namespace[(Namespace.LastIndexOf('.', Namespace.LastIndexOf('.') - 1) + 1)..].Replace(".", "");
}