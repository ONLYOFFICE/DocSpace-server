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

using Microsoft.Extensions.Hosting;

namespace ASC.AppHost.Configuration;

public class ProjectConfigurator(
    IDistributedApplicationBuilder builder,
    ConnectionStringManager connectionManager,
    string basePath,
    bool isDocker)
{
    public static string GetProjectName<TProject>() where TProject : IProjectMetadata, new()
    {
        var name = typeof(TProject).Name.ToLower().Replace('_', '-');
        return name.StartsWith("asc-") ? "onlyoffice-" + name.Substring(4) : name;
    }

    public ProjectConfigurator AddProject<TProject>(int projectPort) where TProject : IProjectMetadata, new()
    {
        if (isDocker)
        {
            AddProjectDocker<TProject>(projectPort);
        }
        else
        {
            AddProjectWithDefaultConfiguration<TProject>();
        }

        return this;
    }

    private void AddProjectWithDefaultConfiguration<TProject>() where TProject : IProjectMetadata, new()
    {
        var project = builder
            .AddProject<TProject>(GetProjectName<TProject>())
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        if (int.TryParse(builder.Configuration["Replicas"], out var replicas) && replicas > 1)
        {
            project.WithReplicas(replicas);
        }
        else
        {
            project.WithEnvironment("core:hosting:singletonMode", true.ToString());
        }

        var isStandalone = string.Compare(builder.Configuration["APP_HOSTING_STANDALONE"], "true", StringComparison.OrdinalIgnoreCase) == 0;

        project.WithEnvironment("core:base-domain", isStandalone ? "localhost" : "")
            .WithEnvironment("ai:mcp:0:endpoint", new UriBuilder(Uri.UriSchemeHttp, "localhost", Constants.DocSpaceMcpPort) + "mcp");

        ConfigureForwardedHeadersNetworks(project);

        // Map the dev HTTPS host to the default standalone tenant.
        project.WithEnvironment("CORE__LOCAL_ADDRESSES", Constants.AppHostHttpsHost);


        switch (builder.Configuration["APP_EDITION"])
        {
            case "enterprise":
            case "developer":
                project.WithEnvironment("license:file:path", Path.Combine(basePath, "Data", "license.lic"))
                    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Configuration["APP_EDITION"]);
                break;
            default:
                project.WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName);

                break;
        }

        connectionManager.AddBaseConfig(project, isDocker);
        connectionManager.AddWaitFor(project);

        if (connectionManager.HasOtelCollector)
        {
            project.WithEnvironment("OTEL_FILE_EXPORTER_ENDPOINT", connectionManager.GetOtelCollectorEndpoint(isDocker: false));
        }
    }

    private void AddProjectDocker<TProject>(int projectPort) where TProject : IProjectMetadata, new()
    {
        var projectMetadata = new TProject();
        var projectBasePath = Path.GetDirectoryName(projectMetadata.ProjectPath) ?? basePath;

        var name = typeof(TProject).Name;
        var resourceBuilder = builder.AddDockerfile(GetProjectName<TProject>(), projectBasePath, stage: "base");

        var netVersion = $"net{Environment.Version.Major}.{Environment.Version.Minor}";
        var dllPath = "/app/bin/Debug/";

        if (Directory.Exists(Path.Combine(projectBasePath, "bin", "Debug", netVersion)))
        {
            dllPath += $"{netVersion}/";
        }

        resourceBuilder
            .WithImageTag("dev")
            .WithBindMount(projectBasePath, "/app")
            .WithEnvironment("log:name", $"/{name.ToLower()["asc-".Length..].Replace('_', '.')}")
            .WithEnvironment("$STORAGE_ROOT", "/data")
            .WithEnvironment("web:hub:internal", new UriBuilder(Uri.UriSchemeHttp, Constants.SocketIoContainer, Constants.SocketIoPort).ToString())
            .WithEnvironment("core:hosting:singletonMode", true.ToString())
            .WithEnvironment("pathToConf", "/buildtools/config/")
            .WithEnvironment("ai:mcp:0:endpoint", new UriBuilder(Uri.UriSchemeHttp, Constants.DocSpaceMcpContainer, Constants.DocSpaceMcpPort).ToString() + "mcp")
            .WithArgs($"{dllPath}{name.Replace('_', '.')}.dll")
            .WithEntrypoint("dotnet");

        switch (builder.Configuration["APP_EDITION"])
        {
            case "enterprise":
            case "developer":
                resourceBuilder.WithEnvironment("license:file:path", "/data/license.lic")
                    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Configuration["APP_EDITION"]);
                break;
            default:
                resourceBuilder.WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName);

                break;
        }


        var isStandalone = string.Compare(builder.Configuration["APP_HOSTING_STANDALONE"], "true", StringComparison.OrdinalIgnoreCase) == 0;

        resourceBuilder.WithEnvironment("core:base-domain", isStandalone ? "localhost" : "");

        ConfigureForwardedHeadersNetworks(resourceBuilder);

        resourceBuilder.WithEnvironment("CORE__LOCAL_ADDRESSES", Constants.AppHostHttpsHost);

        AddBaseBind(resourceBuilder);

        if (projectPort != 0)
        {
            resourceBuilder
                .WithEnvironment("ASPNETCORE_HTTP_PORTS", projectPort.ToString())
                .WithHttpEndpoint(projectPort, projectPort)
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

        connectionManager.AddBaseConfig(resourceBuilder, isDocker);
        connectionManager.AddWaitFor(resourceBuilder);

        var otlEnvs = new Dictionary<string, string> { { "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true" }, { "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true" }, { "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory" } };

        if (resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode && resourceBuilder.ApplicationBuilder.Environment.IsDevelopment())
        {
            otlEnvs.Add("OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION", "true");
            otlEnvs.Add("OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION", "true");
        }

        foreach (var env in otlEnvs)
        {
            resourceBuilder.WithEnvironment(env.Key, env.Value);
        }

        if (connectionManager.HasOtelCollector)
        {
            resourceBuilder.WithEnvironment("OTEL_FILE_EXPORTER_ENDPOINT", connectionManager.GetOtelCollectorEndpoint(isDocker: true));
        }

        resourceBuilder.WithOtlpExporter();
    }

    public ProjectConfigurator AddSocketIO()
    {
        var name = Constants.SocketIoContainer;
        var path = Path.Combine("..", "ASC.Socket.IO");
        var port = Constants.SocketIoPort;

        var redisEnabled = connectionManager.Redis != null;

        if (isDocker)
        {
            var resourceBuilder = builder
                .AddDockerfile(name, path)
                .WithImageTag("dev")
                .WithEnvironment("log:dir", "/logs")
                .WithEnvironment("log:name", "socketIO")
                .WithEnvironment("API_HOST", new UriBuilder(Uri.UriSchemeHttp, Constants.OpenRestyContainer, Constants.RestyPort).ToString())
                .WithEnvironment("Redis:Hosts:0:Host", () => ConnectionStringManager.SubstituteLocalhost(connectionManager.Redis?.Host) ?? string.Empty)
                .WithEnvironment("Redis:Hosts:0:Port", () => connectionManager.Redis?.Port ?? string.Empty)
                .WithEnvironment("REDIS_ENABLED", redisEnabled.ToString().ToLower())
                .WithHttpEndpoint(port, port, isProxied: false)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            AddBaseBind(resourceBuilder);
            connectionManager.AddWaitFor(resourceBuilder);
        }
        else
        {
            var resourceBuilder = builder.AddJavaScriptApp(name, path, "start")
                .WithYarn()
                .WithEnvironment("NODE_ENV", "development")
                .WithEnvironment("API_HOST", $"http://localhost:{Constants.AppHostPort.ToString()}")
                .WithEnvironment("Redis:Hosts:0:Host", () => connectionManager.Redis?.Host ?? string.Empty)
                .WithEnvironment("Redis:Hosts:0:Port", () => connectionManager.Redis?.Port ?? string.Empty)
                .WithEnvironment("REDIS_ENABLED", redisEnabled.ToString().ToLower())
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            connectionManager.AddWaitFor(resourceBuilder);
        }

        return this;
    }

    public ProjectConfigurator AddSsoAuth()
    {
        var name = "onlyoffice-ssoAuth";
        var path = Path.Combine("..", "ASC.SSoAuth");
        var port = Constants.SsoAuthPort;

        if (isDocker)
        {
            var resourceBuilder = builder
                .AddDockerfile(name, path)
                .WithImageTag("dev")
                .WithEnvironment("log:dir", "/logs")
                .WithEnvironment("log:name", "ssoAuth")
                .WithEnvironment("API_HOST", new UriBuilder(Uri.UriSchemeHttp, Constants.OpenRestyContainer, Constants.RestyPort).ToString())
                .WithEnvironment("app:appsettings", "/buildtools/config")
                .WithHttpEndpoint(port, port, isProxied: false)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            AddBaseBind(resourceBuilder);
        }
        else
        {
            builder.AddJavaScriptApp(name, path, "start")
                .WithYarn()
                .WithEnvironment("NODE_ENV", "development")
                .WithEnvironment("API_HOST", $"http://localhost:{Constants.AppHostPort.ToString()}")
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

        return this;
    }

    public ProjectConfigurator AddWebDav()
    {
        var name = "onlyoffice-webDav";
        var path = Path.Combine("..", "ASC.WebDav");
        var port = Constants.WebDavPort;

        if (isDocker)
        {
            var resourceBuilder = builder
                .AddDockerfile(name, path)
                .WithImageTag("dev")
                .WithEnvironment("log:dir", "/logs")
                .WithEnvironment("log:name", "webDav")
                .WithHttpEndpoint(port, port, isProxied: false)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            AddBaseBind(resourceBuilder);
        }
        else
        {
            builder.AddJavaScriptApp(name, path, "start")
                .WithYarn()
                .WithEnvironment("NODE_ENV", "development")
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

        return this;
    }

    public ProjectConfigurator AddIdentity()
    {
        var path = Path.Combine("..", "ASC.Identity");

        var registrationBuilder = builder
            .AddDockerfile(Constants.IdentityRegistrationContainer, path)
            .WithImageTag("dev")
            .WithEnvironment("log:dir", "/logs")
            .WithEnvironment("log:name", "identity.registration")
            .WithEnvironment("SERVER_PORT", Constants.IdentityRegistrationPort.ToString())
            .WithEnvironment("SPRING_PROFILES_ACTIVE", "dev,server")
            .WithEnvironment("SPRING_APPLICATION_SIGNATURE_SECRET", builder.Configuration["core:machinekey"])
            .WithEnvironment("SPRING_APPLICATION_NAME", "ASC.Identity.Registration")
            .WithEnvironment("GRPC_CLIENT_AUTHORIZATION_ADDRESS", new UriBuilder("static", Constants.IdentityAuthorizationContainer, 9999).ToString())
            .WithHttpEndpoint(Constants.IdentityRegistrationPort, Constants.IdentityRegistrationPort, isProxied: false)
            .WithBuildArg("MODULE", "registration/registration-container")
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        connectionManager.AddIdentityEnv(registrationBuilder);

        var authorizationBuilder = builder
            .AddDockerfile(Constants.IdentityAuthorizationContainer, path)
            .WithImageTag("dev")
            .WithEnvironment("log:dir", "/logs")
            .WithEnvironment("log:name", "identity.authorization")
            .WithEnvironment("SERVER_PORT", Constants.IdentityAuthorizationPort.ToString())
            .WithEnvironment("SPRING_PROFILES_ACTIVE", "dev,server")
            .WithEnvironment("SPRING_APPLICATION_SIGNATURE_SECRET", builder.Configuration["core:machinekey"])
            .WithEnvironment("SPRING_APPLICATION_NAME", "ASC.Identity.Authorization")
            .WithEnvironment("GRPC_CLIENT_REGISTRATION_ADDRESS", new UriBuilder("static", Constants.IdentityRegistrationContainer, 8888).ToString())
            .WithHttpEndpoint(Constants.IdentityAuthorizationPort, Constants.IdentityAuthorizationPort, isProxied: false)
            .WithBuildArg("MODULE", "authorization/authorization-container")
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        connectionManager.AddIdentityEnv(authorizationBuilder);

        return this;
    }

    private void AddBaseBind<T>(IResourceBuilder<T> resourceBuilder) where T : ContainerResource
    {
        resourceBuilder
            .WithBindMount(Path.Combine(basePath, "buildtools"), "/buildtools")
            .WithBindMount(Path.Combine(basePath, "Data"), "/data")
            .WithBindMount(Path.Combine(basePath, "Logs"), "/logs")
            .WithEnvironment("log:dir", "/logs");
    }

    private void ConfigureForwardedHeadersNetworks<T>(IResourceBuilder<T> project) where T : IResourceWithEnvironment
    {
        // Defense in depth: this configuration trusts X-Forwarded-* headers from
        // entire RFC1918 ranges, which would let any host on a shared LAN spoof
        // HTTPS/Host. AppHost is dev-only, but guard against accidental reuse
        // outside Development.
        if (!builder.Environment.IsDevelopment())
        {
            return;
        }

        // Loopback always — OpenResty proxies to backends via 127.0.0.1
        // when services run on the host directly.
        project.WithEnvironment("core:hosting:forwardedHeadersOptions:knownNetworks:0", "127.0.0.1/8")
            .WithEnvironment("core:hosting:forwardedHeadersOptions:knownNetworks:1", "::1/128");

        // Docker bridge networks — only needed when backends run in containers
        // and receive traffic from the OpenResty container via the bridge.
        if (isDocker)
        {
            project.WithEnvironment("core:hosting:forwardedHeadersOptions:knownNetworks:2", "10.0.0.0/8")
                .WithEnvironment("core:hosting:forwardedHeadersOptions:knownNetworks:3", "172.16.0.0/12")
                .WithEnvironment("core:hosting:forwardedHeadersOptions:knownNetworks:4", "192.168.0.0/16");
        }
    }
}
