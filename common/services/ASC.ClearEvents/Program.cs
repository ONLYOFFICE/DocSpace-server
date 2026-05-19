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

using HealthChecks.UI.Client;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using NLog;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null
};

var builder = WebApplication.CreateBuilder(options);

builder.Configuration.AddDefaultConfiguration(builder.Environment)
                     .AddEnvironmentVariables()
                     .AddCommandLine(args);

var logger = LogManager.Setup()
                            .SetupExtensions(s =>
                            {
                                s.RegisterLayoutRenderer("application-context", _ => AppName);
                            })
                            .LoadConfiguration(builder.Configuration, builder.Environment)
                            .GetLogger("ASC.ClearEvents");

try
{
    logger.Info("Configuring web host ({applicationContext})...", AppName);

    builder.Host.ConfigureDefault();

    if (builder.Configuration.GetValue<bool>("openTelemetry:enable"))
    {
        builder.ConfigureOpenTelemetry();
    }

    await builder.Services.AddClearEventsServices(builder.Configuration, Namespace);

    builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
    {
        containerBuilder.Register(context.Configuration, false);

        if (string.IsNullOrEmpty(context.Configuration["RabbitMQ:ClientProvidedName"]))
        {
            context.Configuration["RabbitMQ:ClientProvidedName"] = AppName;
        }
    });

    var app = builder.Build();

    app.UseRouting();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).ShortCircuit();

    app.MapHealthChecks("/liveness", new HealthCheckOptions
    {
        Predicate = r => r.Name.Contains("self")
    });

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
    public static readonly string Namespace = "ASC.ClearEvents";
    public static readonly string AppName = Namespace[(Namespace.LastIndexOf('.') + 1)..];
}
