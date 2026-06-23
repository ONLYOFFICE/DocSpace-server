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

using ASC.Core.Common.EF;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
};

var builder = WebApplication.CreateBuilder(options);

builder.Configuration.AddJsonFile($"appsettings.runner.json", true)
                .AddCommandLine(args);

builder.Services.AddScoped<EFLoggerFactory>();
builder.Services.AddBaseDbContext<MigrationContext>();
builder.Services.AddBaseDbContext<TeamlabSiteContext>();

var connectionString = builder.Configuration.GetConnectionString("docspace");


var app = builder.Build();

var providersInfo = app.Configuration.GetSection("options").Get<Options>();
var configurationInfo = !string.IsNullOrEmpty(app.Configuration["standalone"]) ? ConfigurationInfo.Standalone : ConfigurationInfo.SaaS;
var targetMigration = app.Configuration["targetMigration"];
var generateScript = !string.IsNullOrEmpty(app.Configuration["generate-script"]);
var outputPath = app.Configuration["output"];

if (generateScript)
{
    var scripts = new List<string>();

    if (!string.IsNullOrEmpty(connectionString))
    {
        var runner = new MigrationRunner(app.Services);
        scripts.Add(runner.RunGenerateScript(new ProviderInfo
        {
            Provider = Provider.MySql,
            ConnectionString = connectionString,
            ProviderFullName = "MySql.Data.MySqlClient"
        }, configurationInfo, typeof(MigrationContext), targetMigration));
    }
    else
    {
        foreach (var providerInfo in providersInfo.Providers)
        {
            var runner = new MigrationRunner(app.Services);
            scripts.Add(runner.RunGenerateScript(providerInfo, configurationInfo, typeof(MigrationContext), targetMigration));
        }

        foreach (var providerInfo in providersInfo.TeamlabsiteProviders)
        {
            var runner = new MigrationRunner(app.Services);
            scripts.Add(runner.RunGenerateScript(providerInfo, configurationInfo, typeof(TeamlabSiteContext), targetMigration));
        }
    }

    var result = string.Join(Environment.NewLine, scripts);

    if (!string.IsNullOrEmpty(outputPath))
    {
        File.WriteAllText(outputPath, result);
        Console.WriteLine($"Script saved to {outputPath}");
    }
    else
    {
        Console.WriteLine(result);
    }
}
else
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        var migrationCreator = new MigrationRunner(app.Services);
        migrationCreator.RunApplyMigrations(new ProviderInfo
        {
            Provider = Provider.MySql,
            ConnectionString = connectionString,
            ProviderFullName = "MySql.Data.MySqlClient"
        }, configurationInfo, typeof(MigrationContext), targetMigration);
    }
    else
    {
        foreach (var providerInfo in providersInfo.Providers)
        {
            var migrationCreator = new MigrationRunner(app.Services);
            migrationCreator.RunApplyMigrations(providerInfo, configurationInfo, typeof(MigrationContext), targetMigration);
        }

        foreach (var providerInfo in providersInfo.TeamlabsiteProviders)
        {
            var migrationCreator = new MigrationRunner(app.Services);
            migrationCreator.RunApplyMigrations(providerInfo, configurationInfo, typeof(TeamlabSiteContext), targetMigration);
        }
    }

    Console.WriteLine("Migrations applied");
}