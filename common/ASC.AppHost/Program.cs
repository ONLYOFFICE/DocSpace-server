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

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var connectionManager = new ConnectionStringManager(builder)
    .AddMySql()
    .AddRabbitMq()
    .AddRedis()
    .AddEditors();


var basePath = Path.GetFullPath(Path.Combine("..", "..", ".."));
var isDocker = String.Compare(builder.Configuration["Docker"], "true", StringComparison.OrdinalIgnoreCase) == 0;

_ = new ProjectConfigurator(builder, connectionManager, basePath, isDocker)
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
    .AddSocketIO()
    .AddSsoAuth()
    .AddWebDav();

var ascIdentityRegistration = "asc-identity-registration";
var ascIdentityAuthorization = "asc-identity-authorization";

var registrationBuilder = builder
    .AddDockerfile(ascIdentityRegistration, "../ASC.Identity/")
    .WithImageTag("dev")
    .WithEnvironment("log:dir", "/logs")
    .WithEnvironment("log:name", "identity.registration")
    .WithEnvironment("SERVER_PORT", Constants.IdentityRegistrationPort.ToString())
    .WithEnvironment("SPRING_PROFILES_ACTIVE", "dev,server")
    .WithEnvironment("SPRING_APPLICATION_NAME", "ASC.Identity.Registration")
    .WithEnvironment("GRPC_CLIENT_AUTHORIZATION_ADDRESS", $"static://{ascIdentityAuthorization}:9999")
    .WithHttpEndpoint(Constants.IdentityRegistrationPort, Constants.IdentityRegistrationPort, isProxied: false)
    .WithBuildArg("MODULE", "registration/registration-container")
    .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

ASC.AppHost.Extensions.ResourceBuilderExtensions.AddIdentityEnv(registrationBuilder, connectionManager);

var authorizationBuilder = builder
    .AddDockerfile(ascIdentityAuthorization, "../ASC.Identity/")
    .WithImageTag("dev")
    .WithEnvironment("log:dir", "/logs")
    .WithEnvironment("log:name", "identity.authorization")
    .WithEnvironment("SERVER_PORT", Constants.IdentityAuthorizationPort.ToString())
    .WithEnvironment("SPRING_PROFILES_ACTIVE", "dev,server")
    .WithEnvironment("SPRING_APPLICATION_NAME", "ASC.Identity.Authorization")
    .WithEnvironment("GRPC_CLIENT_AUTHORIZATION_ADDRESS", $"static://{ascIdentityRegistration}:8888")
    .WithHttpEndpoint(Constants.IdentityAuthorizationPort, Constants.IdentityAuthorizationPort, isProxied: false)
    .WithBuildArg("MODULE", "authorization/authorization-container")
    .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

ASC.AppHost.Extensions.ResourceBuilderExtensions.AddIdentityEnv(authorizationBuilder, connectionManager);

var clientBasePath = Path.Combine(basePath, "client");
var installPackages = builder.AddExecutable("asc-install-packages", "pnpm", clientBasePath, "install");
var buildPackages = builder.AddExecutable("asc-build-packages", "pnpm", clientBasePath, "build").WaitForCompletion(installPackages);
var startPackages = builder.AddExecutable("asc-start-packages", "pnpm", clientBasePath, "start").WaitForCompletion(buildPackages);
installPackages.WithRelationship(buildPackages.Resource, "Parent");
buildPackages.WithRelationship(startPackages.Resource, "Parent");

NginxConfiguration.ConfigureOpenResty(builder, basePath, clientBasePath, startPackages, isDocker);

await builder.Build().RunAsync();