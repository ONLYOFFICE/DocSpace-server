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

var editorPort = Random.Shared.Next(8086, 8090);
const int restyPort = 8092;

var builder = DistributedApplication.CreateBuilder(args);

var mySql = builder
    .AddMySql("mysql")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("docspace");

var path = Path.GetFullPath(Path.Combine("..", "Tools", "ASC.Migration.Runner", "bin", "Debug", "ASC.Migration.Runner.exe"));

var rabbitMq = builder
    .AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

var redis = builder
    .AddRedis("cache")
    .WithLifetime(ContainerLifetime.Persistent);
    //.WithRedisInsight();

var editors = builder
    .AddContainer("asc-editors", "onlyoffice/documentserver", "latest")
    .WithHttpEndpoint(editorPort, 80)
    .WithEnvironment("JWT_ENABLED", "true")
    .WithEnvironment("JWT_SECRET", "secret")
    .WithEnvironment("JWT_HEADER", "AuthorizationJwt");

var migrate = builder
    .AddExecutable("migrate",path, Path.GetDirectoryName(path) ?? "")
    .WithReference(mySql)
    .WaitFor(mySql);

AddProjectWithDefaultConfiguration<ASC_ApiSystem>();
AddProjectWithDefaultConfiguration<ASC_ClearEvents>();
AddProjectWithDefaultConfiguration<ASC_Data_Backup>();
AddProjectWithDefaultConfiguration<ASC_Data_Backup_BackgroundTasks>();
AddProjectWithDefaultConfiguration<ASC_Notify>(false);
AddProjectWithDefaultConfiguration<ASC_Web_Api>();
AddProjectWithDefaultConfiguration<ASC_People>();
AddProjectWithDefaultConfiguration<ASC_Files_Service>();
AddProjectWithDefaultConfiguration<ASC_Studio_Notify>();
AddProjectWithDefaultConfiguration<ASC_Web_Studio>();

var basePath = Path.GetFullPath(Path.Combine("..", "..", ".."));
var filesBasePath = Path.Combine(basePath, "server", "products", "ASC.Files", "Server");

if (String.Compare(builder.Configuration["Docker"], "true", StringComparison.OrdinalIgnoreCase) == 0)
{
    var filesPort = 5007;
    builder
    .AddDockerfile("asc-files", filesBasePath, stage: "base")
    .WithBindMount(filesBasePath, "/app")
    .WithBindMount(Path.Combine(basePath, "buildtools"), "/buildtools")
    .WithBindMount(Path.Combine(basePath, "Data"), "/data")
    .WithBindMount(Path.Combine(basePath, "Logs"), "/logs")
    .WithEnvironment("log:dir", "/logs")
    .WithEnvironment("log:name", "/files")
    .WithEnvironment("$STORAGE_ROOT", "/data")
    .WithEnvironment("files:docservice:url:portal", $"http://host.docker.internal:{restyPort.ToString()}")
    .WithEnvironment("files:docservice:url:portal", $"http://localhost:{editorPort.ToString()}")
    .WithEnvironment("ASPNETCORE_HTTP_PORTS", filesPort.ToString())
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WithReference(redis, "redis")
    .WaitFor(migrate)
    .WaitFor(rabbitMq)
    .WaitFor(redis)
    .WaitFor(editors)
    .WithArgs("/app/bin/Debug/net9.0/ASC.Files.dll")
    .WithEntrypoint("dotnet")
    .WithHttpEndpoint(filesPort, filesPort, isProxied:false);
}
else
{
    AddProjectWithDefaultConfiguration<ASC_Files>();
}





builder.AddNpmApp("asc-socketIO", "../ASC.Socket.IO/", "start:build").WithHttpEndpoint(targetPort: 9899).WithHttpHealthCheck("/health");
builder.AddNpmApp("asc-ssoAuth", "../ASC.SSoAuth/", "start:build").WithHttpEndpoint(targetPort: 9834).WithHttpHealthCheck("/health");
builder.AddNpmApp("asc-webDav", "../ASC.WebDav/", "start:build").WithHttpEndpoint(targetPort: 1900).WithHttpHealthCheck("/health");

var clientBasePath = Path.Combine(basePath, "client");
var installPackages = builder.AddExecutable("asc-install-packages", "yarn", clientBasePath, "install");
var buildPackages = builder.AddExecutable("asc-build-packages", "yarn", clientBasePath, "build").WaitForCompletion(installPackages);
var startPackages = builder.AddExecutable("asc-start-packages", "yarn", clientBasePath, "start").WaitForCompletion(buildPackages);
installPackages.WithRelationship(buildPackages.Resource, "Parent");
buildPackages.WithRelationship(startPackages.Resource, "Parent");

builder.AddContainer("asc-openresty", "openresty/openresty", "latest")
    .WithBindMount(Path.Combine(basePath, "buildtools", "config", "nginx"), "/etc/nginx/conf.d/")
    .WithBindMount(Path.Combine(basePath, "buildtools", "config", "nginx", "includes"), "/etc/nginx/includes/")
    .WithBindMount(Path.Combine(clientBasePath, "public"), "/var/www/public")
    .WithBindMount(Path.Combine(clientBasePath, "packages", "client"), "/var/www/client")
    .WithBindMount(Path.Combine(clientBasePath, "packages", "login"), "/var/www/login")
    .WithBindMount(Path.Combine(clientBasePath, "packages", "management"), "/var/www/management")
    .WithHttpEndpoint(restyPort, restyPort)
    .WaitFor(startPackages);

await builder.Build().RunAsync();

return;

void AddProjectWithDefaultConfiguration<TProject>(bool includeHealthCheck = true) where TProject : IProjectMetadata, new()
{
    var project = builder.AddProject<TProject>(typeof(TProject).Name.ToLower().Replace('_', '-'));
    
    if (includeHealthCheck)
    {
        project.WithHttpHealthCheck("/health");
    }

    project
        .WithReference(mySql, "default:connectionString")
        .WithReference(rabbitMq, "rabbitMQ")
        .WithReference(redis, "redis")
        .WithEnvironment("files:docservice:url:portal", $"http://host.docker.internal:{restyPort.ToString()}")
        .WithEnvironment("files:docservice:url:public", $"http://localhost:{editorPort.ToString()}")
        .WaitFor(migrate)
        .WaitFor(rabbitMq)
        .WaitFor(redis)
        .WaitFor(editors);
}