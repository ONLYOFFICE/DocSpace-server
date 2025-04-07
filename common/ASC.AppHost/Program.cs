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

using MySqlConnector;

using Projects;

var editorPort = Random.Shared.Next(8086, 8090);
const int restyPort = 8092;
const int socketIoPort = 9899;
const int ssoAuthPort = 9834;
const int webDavPort = 1900;
const int identityRegistrationPort = 8080;
const int identityAuthorizationPort = 9090;

var builder = DistributedApplication.CreateBuilder(args);

var mySql = builder
    .AddMySql("mysql")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("docspace");

MySqlConnectionStringBuilder? mySqlConnectionStringBuilder = null;
builder.Eventing.Subscribe(mySql.Resource, (Func<ConnectionStringAvailableEvent, CancellationToken, Task>) (async (_, ct) =>
{
    var connectionString = await mySql.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
    if (connectionString != null && mySqlConnectionStringBuilder == null)
    {
        mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
    }
}));

var path = Path.GetFullPath(Path.Combine("..", "Tools", "ASC.Migration.Runner", "bin", "Debug", "ASC.Migration.Runner.exe"));

var rabbitMq = builder
    .AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

Uri? rabbitMqUri = null;
builder.Eventing.Subscribe(rabbitMq.Resource, (Func<ConnectionStringAvailableEvent, CancellationToken, Task>) (async (_, ct) =>
{
    var connectionString = await rabbitMq.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
    if (connectionString != null && rabbitMqUri == null && Uri.IsWellFormedUriString(connectionString, UriKind.Absolute))
    {
        rabbitMqUri = new Uri(connectionString);
    }
}));

var redis = builder
    .AddRedis("cache")
    .WithLifetime(ContainerLifetime.Persistent);
    //.WithRedisInsight();

string? redisHost = null;
string? redisPort = null;
builder.Eventing.Subscribe(redis.Resource, (Func<ConnectionStringAvailableEvent, CancellationToken, Task>) (async (_, ct) =>
{
    var connectionString = await redis.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
    if (connectionString != null)
    {
        var splitted = connectionString.Split(':');
        if (splitted.Length == 2)
        {
            redisHost = splitted[0];
            redisPort = splitted[1];
        }
    }
}));
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

var basePath = Path.GetFullPath(Path.Combine("..", "..", ".."));

if (String.Compare(builder.Configuration["Docker"], "true", StringComparison.OrdinalIgnoreCase) == 0)
{
    AddProjectDocker<ASC_Files>(5007);
    AddProjectDocker<ASC_People>(5004);
    AddProjectDocker<ASC_Web_Api>(5000);
    AddProjectDocker<ASC_ApiSystem>(5010);
    AddProjectDocker<ASC_ClearEvents>(5027);
    AddProjectDocker<ASC_Data_Backup>(5012);
    AddProjectDocker<ASC_Data_Backup_BackgroundTasks>(5032);
    AddProjectDocker<ASC_Notify>(0, false);
    AddProjectDocker<ASC_Files_Service>(5009);
    AddProjectDocker<ASC_Studio_Notify>(5006);
    AddProjectDocker<ASC_Web_Studio>(5003);
    
    builder.AddDockerfile("asc-socketIO", "../ASC.Socket.IO/")
        .WithImageTag("dev")
        .WithBindMount(Path.Combine(basePath, "buildtools"), "/buildtools")
        .WithBindMount(Path.Combine(basePath, "Data"), "/data")
        .WithBindMount(Path.Combine(basePath, "Logs"), "/logs")
        .WithEnvironment("log:dir", "/logs")
        .WithEnvironment("log:name", "socketIO")
        .WithReference(redis, "redis")
        .WithHttpEndpoint(socketIoPort, socketIoPort, isProxied: false)
        .WithHttpHealthCheck("/health");
    
    builder.AddDockerfile("asc-ssoAuth", "../ASC.SSoAuth/")
        .WithImageTag("dev")
        .WithBindMount(Path.Combine(basePath, "buildtools"), "/buildtools")
        .WithBindMount(Path.Combine(basePath, "Data"), "/data")
        .WithBindMount(Path.Combine(basePath, "Logs"), "/logs")
        .WithEnvironment("log:dir", "/logs")
        .WithEnvironment("log:name", "ssoAuth")
        .WithEnvironment("app:appsettings", "/buildtools/config")
        .WithHttpEndpoint(ssoAuthPort, ssoAuthPort, isProxied: false)
        .WithHttpHealthCheck("/health");
    
    builder.AddDockerfile("asc-webDav", "../ASC.WebDav/")
        .WithImageTag("dev")
        .WithBindMount(Path.Combine(basePath, "buildtools"), "/buildtools")
        .WithBindMount(Path.Combine(basePath, "Data"), "/data")
        .WithBindMount(Path.Combine(basePath, "Logs"), "/logs")
        .WithEnvironment("log:dir", "/logs")
        .WithEnvironment("log:name", "webDav")
        .WithHttpEndpoint(webDavPort, webDavPort, isProxied: false)
        .WithHttpHealthCheck("/health");
}
else
{
    AddProjectWithDefaultConfiguration<ASC_Files>();
    AddProjectWithDefaultConfiguration<ASC_People>();
    AddProjectWithDefaultConfiguration<ASC_Web_Api>();
    AddProjectWithDefaultConfiguration<ASC_ApiSystem>();
    AddProjectWithDefaultConfiguration<ASC_ClearEvents>();
    AddProjectWithDefaultConfiguration<ASC_Data_Backup>();
    AddProjectWithDefaultConfiguration<ASC_Data_Backup_BackgroundTasks>();
    AddProjectWithDefaultConfiguration<ASC_Notify>(false);
    AddProjectWithDefaultConfiguration<ASC_Files_Service>();
    AddProjectWithDefaultConfiguration<ASC_Studio_Notify>();
    AddProjectWithDefaultConfiguration<ASC_Web_Studio>();
    
    builder.AddNpmApp("asc-socketIO", "../ASC.Socket.IO/", "start:build").WithReference(redis, "redis").WithHttpEndpoint(targetPort: socketIoPort).WithHttpHealthCheck("/health");
    builder.AddNpmApp("asc-ssoAuth", "../ASC.SSoAuth/", "start:build").WithHttpEndpoint(targetPort: 9834).WithHttpHealthCheck("/health");
    builder.AddNpmApp("asc-webDav", "../ASC.WebDav/", "start:build").WithHttpEndpoint(targetPort: 1900).WithHttpHealthCheck("/health");
}

var registrationBuilder = builder.AddDockerfile("asc-identity-registration", "../ASC.Identity/")
    .WithImageTag("dev")
    .WithEnvironment("log:dir", "/logs")
    .WithEnvironment("log:name", "identity.registration")
    .WithEnvironment("SERVER_PORT", identityRegistrationPort.ToString())
    .WithEnvironment("SPRING_PROFILES_ACTIVE", "dev,server")
    .WithEnvironment("SPRING_APPLICATION_NAME", "ASC.Identity.Registration")
    .WithEnvironment("GRPC_CLIENT_AUTHORIZATION_ADDRESS", "static://authorization-service:9999")
    .WithHttpEndpoint(identityRegistrationPort, identityRegistrationPort, isProxied: false)
    .WithBuildArg("MODULE", "registration/registration-container");

AddIdentityEnv(registrationBuilder);

var authorizationBuilder = builder.AddDockerfile("asc-identity-authorization", "../ASC.Identity/")
    .WithImageTag("dev")
    .WithEnvironment("log:dir", "/logs")
    .WithEnvironment("log:name", "identity.authorization")
    .WithEnvironment("SERVER_PORT", identityAuthorizationPort.ToString())
    .WithEnvironment("SPRING_PROFILES_ACTIVE", "dev,server")
    .WithEnvironment("SPRING_APPLICATION_NAME", "ASC.Identity.Authorization")
    .WithEnvironment("GRPC_CLIENT_AUTHORIZATION_ADDRESS", "static://registration-service:8888")
    .WithHttpEndpoint(identityAuthorizationPort, identityAuthorizationPort, isProxied: false)
    .WithBuildArg("MODULE", "authorization/authorization-container");

AddIdentityEnv(authorizationBuilder);

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
    AddBaseConfig(project, includeHealthCheck);
}

void AddProjectDocker<TProject>(int projectPort, bool includeHealthCheck = true) where TProject : IProjectMetadata, new()
{
    var projectMetadata = new TProject();
    var projectBasePath = Path.GetDirectoryName(projectMetadata.ProjectPath) ?? basePath;
    
    var name = typeof(TProject).Name;
    var resourceBuilder = builder.AddDockerfile(name.ToLower().Replace('_', '-'), projectBasePath, stage: "base");
    AddBaseConfig(resourceBuilder, includeHealthCheck);

    var dllPath = "/app/bin/Debug/";
    if (Directory.Exists(Path.Combine(projectBasePath, "bin", "Debug", "net9.0")))
    {
        dllPath += "net9.0/";
    }
    
    resourceBuilder
        .WithImageTag("dev")
        .WithBindMount(projectBasePath, "/app")
        .WithBindMount(Path.Combine(basePath, "buildtools"), "/buildtools")
        .WithBindMount(Path.Combine(basePath, "Data"), "/data")
        .WithBindMount(Path.Combine(basePath, "Logs"), "/logs")
        .WithEnvironment("log:dir", "/logs")
        .WithEnvironment("log:name", $"/{name.ToLower()["asc-".Length..].Replace('_', '.')}")
        .WithEnvironment("$STORAGE_ROOT", "/data")
        .WithEnvironment("web:hub:internal", "http://asc-socketIO:9899")
        .WithArgs($"{dllPath}{name.Replace('_', '.')}.dll")
        .WithEntrypoint("dotnet");

    if (projectPort != 0)
    {
        resourceBuilder
            .WithEnvironment("ASPNETCORE_HTTP_PORTS", projectPort.ToString())
            .WithHttpEndpoint(projectPort, projectPort);
    }
}

void AddBaseConfig<T>(IResourceBuilder<T> resourceBuilder, bool includeHealthCheck = true) where T : IResourceWithEnvironment, IResourceWithWaitSupport, IResourceWithEndpoints
{
    if (includeHealthCheck)
    {
        resourceBuilder.WithHttpHealthCheck("/health");
    }

    resourceBuilder
        .WithEnvironment("files:docservice:url:portal", $"http://host.docker.internal:{restyPort.ToString()}")
        .WithEnvironment("files:docservice:url:public", $"http://localhost:{editorPort.ToString()}")
        .WithReference(mySql, "default:connectionString")
        .WithReference(rabbitMq, "rabbitMQ")
        .WithReference(redis, "redis");

    AddWaitFor(resourceBuilder);
}

void AddWaitFor<T>(IResourceBuilder<T> resourceBuilder, bool includeMigrate = true, bool includeRabbitMq = true, bool includeRedis = true, bool includeEditors = true) where T : IResourceWithWaitSupport
{
    if (includeMigrate)
    {
        resourceBuilder.WaitFor(migrate);
    }
    if (includeRabbitMq)
    {
        resourceBuilder.WaitFor(rabbitMq);
    }
    if (includeRedis)
    {
        resourceBuilder.WaitFor(redis);
    }
    if (includeEditors)
    {
        resourceBuilder.WaitFor(editors);
    }
}

void AddIdentityEnv<T>(IResourceBuilder<T> resourceBuilder) where T : ContainerResource
{
    resourceBuilder
        .WithEnvironment("JDBC_URL", () => mySqlConnectionStringBuilder != null ? $"{mySqlConnectionStringBuilder.Server.Replace("localhost", "host.docker.internal")}:{mySqlConnectionStringBuilder.Port}" : string.Empty)
        .WithEnvironment("JDBC_DATABASE", () => mySqlConnectionStringBuilder != null ? $"{mySqlConnectionStringBuilder.Database}" : string.Empty)
        .WithEnvironment("JDBC_USER_NAME", () => mySqlConnectionStringBuilder != null ? $"{mySqlConnectionStringBuilder.UserID}" : string.Empty)
        .WithEnvironment("JDBC_PASSWORD", () => mySqlConnectionStringBuilder != null ? $"{mySqlConnectionStringBuilder.Password}" : string.Empty);
    
    resourceBuilder
        .WithEnvironment("RABBIT_HOST", () => rabbitMqUri != null ? $"{rabbitMqUri.Host.Replace("localhost", "host.docker.internal")}" : string.Empty)
        .WithEnvironment("RABBIT_URI", () => rabbitMqUri != null ? rabbitMqUri.ToString().Replace("localhost", "host.docker.internal") : string.Empty);
    
    resourceBuilder
        .WithEnvironment("REDIS_HOST", () => redisHost?.Replace("localhost", "host.docker.internal") ?? string.Empty)
        .WithEnvironment("REDIS_PORT", () => redisPort ?? string.Empty);
    
    AddWaitFor(resourceBuilder, includeEditors: false);
}