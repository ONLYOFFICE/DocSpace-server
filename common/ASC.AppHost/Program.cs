// (c) Copyright Ascensio System SIA 2009-2026
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

using Aspire.Hosting.JavaScript;

#pragma warning disable ASPIREINTERACTION001

var builder = DistributedApplication.CreateBuilder(args);
IResourceBuilder<JavaScriptAppResource>? playwright = null;
var basePath = Path.GetFullPath(Path.Combine("..", "..", ".."));
var isDocker = String.Compare(builder.Configuration["Docker"], "true", StringComparison.OrdinalIgnoreCase) == 0;
var skipClient = String.Compare(builder.Configuration["SKIP_CLIENT"], "true", StringComparison.OrdinalIgnoreCase) == 0;

var connectionManager = new ConnectionStringManager(builder, basePath)
    .AddRabbitMq()
    .AddEditors();

var configurator = new ProjectConfigurator(builder, connectionManager, basePath, isDocker);

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];
switch (launchProfile)
{
    case "preview":
        connectionManager.AddMySql()
            .AddRedis();
        configurator
            .AddProject<ASC_Files>(Constants.FilesPort)
            .AddProject<ASC_Files_Worker>(Constants.FilesWorkerPort)
            .AddProject<ASC_People>(Constants.PeoplePort)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort)
            .AddProject<ASC_Web_Studio>(Constants.WebstudioPort)
            .AddProject<ASC_AI>(Constants.AiPort)
            .AddProject<ASC_AI_Worker>(Constants.AiWorkerPort)
            .AddSocketIO();

        break;
    case "frontend-dev":
        connectionManager.AddMySql(withDbGate: true)
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
        connectionManager.AddMySql(withDbGate: true)
            .AddRedis(withRedisInsight: true)
            .AddMcpServer()
            .AddOpensearch()
            .AddMailPit();

        Dictionary<string, string>? apiSystemVariables = null;
        Dictionary<string, string>? additionalSystemVariables = null;
        if (launchProfile == "test")
        {
            var docspaceOwnerEmail = builder.Configuration["OWNER_EMAIL"];
            var coreMachineKey = builder.Configuration["core:machinekey"] ?? "test-machine-key";
            
            var playwrightTestsPath = Path.Combine(basePath, "test", "api");

            playwright = builder.AddJavaScriptApp("playwright-tests", playwrightTestsPath, "test")
                .WithNpm()
                .WithEnvironment("MACHINEKEY", coreMachineKey)
                .WithEnvironment("PKEY", "PKEY")
                .WithEnvironment("LOCAL_PORTAL_DOMAIN", $"localhost:{Constants.AppHostPort.ToString()}")
                .WithEnvironment("DOCSPACE_OWNER_EMAIL", docspaceOwnerEmail);
            
            additionalSystemVariables= new Dictionary<string, string>
            {
                { "web:autotest:secret-email", docspaceOwnerEmail },
                { "core:machinekey", coreMachineKey }
            };
            
            apiSystemVariables = new Dictionary<string, string>(additionalSystemVariables)
            {
                { "auth:allowskip:default", true.ToString() },
                { "auth:allowskip:registerportal", true.ToString() }
            };
        }
        
        configurator
            .AddProject<ASC_Files>(Constants.FilesPort, additionalSystemVariables)
            .AddProject<ASC_Files_Worker>(Constants.FilesWorkerPort, additionalSystemVariables)
            .AddProject<ASC_People>(Constants.PeoplePort, additionalSystemVariables)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort, additionalSystemVariables)
            .AddProject<ASC_ApiSystem>(Constants.ApiSystemPort, apiSystemVariables)
            .AddProject<ASC_ClearEvents>(Constants.ClearEventsPort, additionalSystemVariables)
            .AddProject<ASC_Data_Backup>(Constants.BackupPort, additionalSystemVariables)
            .AddProject<ASC_Data_Backup_Worker>(Constants.BackupWorkerPort, additionalSystemVariables)
            .AddProject<ASC_Notify>(Constants.NotifyPort, additionalSystemVariables)
            .AddProject<ASC_Studio_Notify>(Constants.StudioNotifyPort, additionalSystemVariables)
            .AddProject<ASC_Web_Studio>(Constants.WebstudioPort, additionalSystemVariables)
            .AddProject<ASC_AI>(Constants.AiPort, additionalSystemVariables)
            .AddProject<ASC_AI_Worker>(Constants.AiWorkerPort, additionalSystemVariables)
            .AddProject<ASC_TelegramService>(Constants.TelegramPort, additionalSystemVariables)
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
    startPackages = builder.AddJavaScriptApp("onlyoffice-client", clientBasePath, "start").WithPnpm();
}

var openresty = NginxConfiguration.ConfigureOpenResty(builder, basePath, clientBasePath, startPackages, isDocker);

playwright?.WaitFor(openresty);

await builder.Build().RunAsync();