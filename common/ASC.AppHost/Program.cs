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

#pragma warning disable ASPIREINTERACTION001

var builder = DistributedApplication.CreateBuilder(args);
IResourceBuilder<JavaScriptAppResource>? playwright = null;
var basePath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", ".."));
var isDocker = string.Compare(builder.Configuration["Docker"], "true", StringComparison.OrdinalIgnoreCase) == 0;
var skipClient = string.Compare(builder.Configuration["SKIP_CLIENT"], "true", StringComparison.OrdinalIgnoreCase) == 0;
var storybook = string.Compare(builder.Configuration["STORYBOOK"], "true", StringComparison.OrdinalIgnoreCase) == 0;

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];
var otelFileLogging = string.Compare(builder.Configuration["OTEL_FILE_LOGGING"], "true", StringComparison.OrdinalIgnoreCase) == 0;
var connectionManager = new ConnectionStringManager(builder, basePath).AddEditors();

if (otelFileLogging)
{
    connectionManager.AddOpenTelemetryCollector();
}

var configurator = new ProjectConfigurator(builder, connectionManager, basePath, isDocker);
switch (launchProfile)
{
    case "integration-test":
        connectionManager
            .AddMySql(withDataVolume: false)
            .AddRabbitMq()
            .AddRedis()
            .AddOpensearch(withDashboard: false, fixedPort: false, withDataVolume: false);

        configurator
            .AddProject<ASC_Files>(Constants.FilesPort)
            .AddProject<ASC_Files_Worker>(Constants.FilesWorkerPort)
            .AddProject<ASC_People>(Constants.PeoplePort)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort);

        break;
    case "preview":
        connectionManager
            .AddMySql();

        configurator
            .AddProject<ASC_Monolith>(Constants.MonolithPort)
            .AddSocketIO();

        break;
    case "frontend-dev":
        connectionManager.AddMySql(withDbGate: true)
            .AddRabbitMq()
            .AddRedis()
            .AddMailPit()
            .AddMcpServer();

        configurator
            .AddProject<ASC_Files>(Constants.FilesPort)
            .AddProject<ASC_Files_Worker>(Constants.FilesWorkerPort)
            .AddProject<ASC_People>(Constants.PeoplePort)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort)
            .AddProject<ASC_ApiSystem>(Constants.ApiSystemPort)
            .AddProject<ASC_Data_Backup>(Constants.BackupPort)
            .AddProject<ASC_Data_Backup_Worker>(Constants.BackupWorkerPort)
            .AddProject<ASC_Notify>(Constants.NotifyPort)
            .AddProject<ASC_Studio_Notify>(Constants.StudioNotifyPort)
            .AddProject<ASC_Web_Studio>(Constants.WebstudioPort)
            .AddProject<ASC_AI>(Constants.AiPort)
            .AddProject<ASC_AI_Worker>(Constants.AiWorkerPort)
            .AddProject<ASC_TelegramService>(Constants.TelegramPort)
            .AddSocketIO()
            .AddSsoAuth();

        break;
    default:
        connectionManager
            .AddMySql(withDbGate: true)
            .AddRabbitMq()
            .AddRedis(withRedisInsight: true)
            .AddMcpServer()
            .AddOpensearch()
            .AddMailPit();

        if (launchProfile == "test")
        {
            connectionManager
                .AddApiTest()
                .AddE2ETest();
        }

        configurator
            .AddProject<ASC_Files>(Constants.FilesPort)
            .AddProject<ASC_Files_Worker>(Constants.FilesWorkerPort)
            .AddProject<ASC_People>(Constants.PeoplePort)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort)
            .AddProject<ASC_ApiSystem>(Constants.ApiSystemPort)
            .AddProject<ASC_ClearEvents>(Constants.ClearEventsPort)
            .AddProject<ASC_Data_Backup>(Constants.BackupPort)
            .AddProject<ASC_Data_Backup_Worker>(Constants.BackupWorkerPort)
            .AddProject<ASC_Notify>(Constants.NotifyPort)
            .AddProject<ASC_Studio_Notify>(Constants.StudioNotifyPort)
            .AddProject<ASC_Web_Studio>(Constants.WebstudioPort)
            .AddProject<ASC_AI>(Constants.AiPort)
            .AddProject<ASC_AI_Worker>(Constants.AiWorkerPort)
            .AddProject<ASC_TelegramService>(Constants.TelegramPort)
            .AddSocketIO()
            .AddSsoAuth()
            .AddWebDav()
            .AddIdentity();

        break;
}

IResourceBuilder<ExecutableResource>? startPackages = null;

var clientBasePath = Path.Combine(basePath, "client");

if (!skipClient)
{
    var certDir = DevCertificateGenerator.EnsureCertificate(basePath);
    var dnsPatchPath = Path.Combine(builder.AppHostDirectory, "scripts", "docspace-dns-patch.js").Replace('\\', '/');
    var crtPath = Path.Combine(certDir, DevCertificateGenerator.CrtFileName);

    startPackages = builder.AddJavaScriptApp("onlyoffice-client", clientBasePath, "start")
        .WithPnpm()
        .WithEnvironment("NODE_OPTIONS", $"--require={dnsPatchPath}")
        .WithEnvironment("NODE_EXTRA_CA_CERTS", crtPath)
        .WithEnvironment("API_HOST", $"http://localhost:{Constants.AppHostPort.ToString()}");

    if (storybook)
    {
        builder.AddJavaScriptApp("onlyoffice-storybook", Path.Combine(clientBasePath, "libs", "ui-kit"), "storybook")
            .WithPnpm(false)
            .WithEnvironment("STORYBOOK_PROXY", "true")
            .WithEnvironment("BROWSER", "none");
    }
}

var isPreview = builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "preview";
var openresty = NginxConfiguration.ConfigureOpenResty(builder, basePath, clientBasePath, startPackages, isDocker, isPreview);

playwright?.WaitFor(openresty);

await builder.Build().RunAsync();
