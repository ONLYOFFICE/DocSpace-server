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

var builder = DistributedApplication.CreateBuilder(args);

var basePath = Path.GetFullPath(Path.Combine("..", "..", ".."));
var isDocker = String.Compare(builder.Configuration["Docker"], "true", StringComparison.OrdinalIgnoreCase) == 0;
var isPreview = String.Compare(builder.Configuration["ASPNETCORE_ENVIRONMENT"], "Preview", StringComparison.OrdinalIgnoreCase) == 0;
var skipClient = String.Compare(builder.Configuration["SKIP_CLIENT"], "true", StringComparison.OrdinalIgnoreCase) == 0;

var connectionManager = new ConnectionStringManager(builder)
    .AddMySql()
    .AddRabbitMq()
    .AddRedis()
    .AddEditors();

if (!isPreview)
{
    connectionManager
        .AddOpensearch()
        .AddMailPit();
}

var configurator = new ProjectConfigurator(builder, connectionManager, basePath, isDocker);


switch (builder.Configuration["ASPNETCORE_ENVIRONMENT"])
{
    case "Preview":
        configurator
            .AddProject<ASC_Files>(Constants.FilesPort)
            .AddProject<ASC_Files_Service>(Constants.FilesServicePort)
            .AddProject<ASC_People>(Constants.PeoplePort)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort)
            .AddProject<ASC_Web_Studio>(Constants.WebstudioPort)
            .AddProject<ASC_AI>(Constants.AiPort)
            .AddProject<ASC_AI_Service>(Constants.AiServicePort)
            .AddSocketIO();

        break;
    case "FrontendDev":
        configurator
            .AddProject<ASC_Files>(Constants.FilesPort)
            .AddProject<ASC_Files_Service>(Constants.FilesServicePort)
            .AddProject<ASC_People>(Constants.PeoplePort)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort)
            .AddProject<ASC_ApiSystem>(Constants.ApiSystemPort)
            .AddProject<ASC_Data_Backup>(Constants.BackupPort)
            .AddProject<ASC_Data_Backup_BackgroundTasks>(Constants.BackupBackgroundTasksPort)
            .AddProject<ASC_Notify>(0, false)
            .AddProject<ASC_Studio_Notify>(Constants.StudioNotifyPort)
            .AddProject<ASC_Web_Studio>(Constants.WebstudioPort)
            .AddProject<ASC_AI>(Constants.AiPort)
            .AddProject<ASC_AI_Service>(Constants.AiServicePort)
            .AddProject<ASC_TelegramService>(0, false)
            .AddSocketIO()
            .AddSsoAuth();

        break;
    default:
        configurator
            .AddProject<ASC_Files>(Constants.FilesPort)
            .AddProject<ASC_Files_Service>(Constants.FilesServicePort)
            .AddProject<ASC_People>(Constants.PeoplePort)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort)
            .AddProject<ASC_ApiSystem>(Constants.ApiSystemPort)
            .AddProject<ASC_ClearEvents>(Constants.ClearEventsPort)
            .AddProject<ASC_Data_Backup>(Constants.BackupPort)
            .AddProject<ASC_Data_Backup_BackgroundTasks>(Constants.BackupBackgroundTasksPort)
            .AddProject<ASC_Notify>(0, false)
            .AddProject<ASC_Studio_Notify>(Constants.StudioNotifyPort)
            .AddProject<ASC_Web_Studio>(Constants.WebstudioPort)
            .AddProject<ASC_AI>(Constants.AiPort)
            .AddProject<ASC_AI_Service>(Constants.AiServicePort)
            .AddProject<ASC_TelegramService>(0, false)
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
    var installPackages = builder.AddExecutable("asc-install-packages", "pnpm", clientBasePath, "install");
    var buildPackages = builder.AddExecutable("asc-build-packages", "pnpm", clientBasePath, "build").WaitForCompletion(installPackages);

    startPackages = builder.AddExecutable("asc-start-packages", "pnpm", clientBasePath, "start").WaitForCompletion(buildPackages);
    installPackages.WithChildRelationship(buildPackages);
    buildPackages.WithChildRelationship(startPackages);
}

NginxConfiguration.ConfigureOpenResty(builder, basePath, clientBasePath, startPackages, isDocker);

await builder.Build().RunAsync();