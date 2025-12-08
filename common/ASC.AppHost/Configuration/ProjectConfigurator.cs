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

using Microsoft.Extensions.Hosting;

namespace ASC.AppHost.Configuration;

public class ProjectConfigurator(
    IDistributedApplicationBuilder builder,
    ConnectionStringManager connectionManager,
    string basePath,
    bool isDocker)
{
    public static string GetProjectName<TProject>() where TProject : IProjectMetadata, new() =>
        typeof(TProject).Name.ToLower().Replace('_', '-');
    
    public ProjectConfigurator AddProject<TProject>(int projectPort, bool includeHealthCheck = true) where TProject : IProjectMetadata, new()
    {
        if (isDocker)
        {
            AddProjectDocker<TProject>(projectPort, includeHealthCheck);
        }
        else
        {
            AddProjectWithDefaultConfiguration<TProject>(includeHealthCheck);
        }

        return this;
    }

    private void AddProjectWithDefaultConfiguration<TProject>(bool includeHealthCheck = true) where TProject : IProjectMetadata, new()
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
        
        connectionManager.AddBaseConfig(project, isDocker, includeHealthCheck);
        connectionManager.AddWaitFor(project);
    }

    private void AddProjectDocker<TProject>(int projectPort, bool includeHealthCheck = true) where TProject : IProjectMetadata, new()
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
            .WithArgs($"{dllPath}{name.Replace('_', '.')}.dll")
            .WithEntrypoint("dotnet");

        AddBaseBind(resourceBuilder);

        if (projectPort != 0)
        {
            resourceBuilder
                .WithEnvironment("ASPNETCORE_HTTP_PORTS", projectPort.ToString())
                .WithHttpEndpoint(projectPort, projectPort)
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

        connectionManager.AddBaseConfig(resourceBuilder, isDocker, includeHealthCheck);
        connectionManager.AddWaitFor(resourceBuilder);

        var otlEnvs = new Dictionary<string, string>
        {
            {"OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true" },
            {"OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true"},
            {"OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory"}
        };

        if (resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode && resourceBuilder.ApplicationBuilder.Environment.IsDevelopment())
        {
            otlEnvs.Add("OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION", "true");
            otlEnvs.Add("OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION", "true");
        }

        foreach (var env in otlEnvs)
        {
            resourceBuilder.WithEnvironment(env.Key, env.Value);
        }
        
        resourceBuilder.WithOtlpExporter();
    }

    public ProjectConfigurator AddSocketIO()
    {
        var name = Constants.SocketIoContainer;
        var path = "../ASC.Socket.IO/";
        var port = Constants.SsoAuthPort;
        
        if (isDocker)
        {
            var resourceBuilder = builder
                .AddDockerfile(name, path)
                .WithImageTag("dev")
                .WithEnvironment("log:name", "socketIO")
                .WithEnvironment("API_HOST", new UriBuilder(Uri.UriSchemeHttp, Constants.OpenRestyContainer, Constants.RestyPort).ToString())
                .WithEnvironment("Redis:Hosts:0:Host", () => ConnectionStringManager.SubstituteLocalhost(connectionManager.RedisHost) ?? string.Empty)
                .WithEnvironment("Redis:Hosts:0:Port", () => connectionManager.RedisPort ?? string.Empty)
                .WithHttpEndpoint(port, port, isProxied: false)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            AddBaseBind(resourceBuilder);
        }
        else
        {
            builder.AddNpmApp(name, path, "start:build")
                .WithEnvironment("Redis:Hosts:0:Host", () => connectionManager.RedisHost ?? string.Empty)
                .WithEnvironment("Redis:Hosts:0:Port", () => connectionManager.RedisPort ?? string.Empty)
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

        return this;
    }

    public ProjectConfigurator AddSsoAuth()
    {
        var name = "asc-ssoAuth";
        var path = "../ASC.SSoAuth/";
        var port = Constants.SsoAuthPort;
        
        if (isDocker)
        {
            var resourceBuilder = builder
                .AddDockerfile(name, path)
                .WithImageTag("dev")
                .WithEnvironment("log:name", "ssoAuth")
                .WithEnvironment("API_HOST",  new UriBuilder(Uri.UriSchemeHttp, Constants.OpenRestyContainer, Constants.RestyPort).ToString())
                .WithEnvironment("app:appsettings", "/buildtools/config")
                .WithHttpEndpoint(port, port, isProxied: false)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            AddBaseBind(resourceBuilder);
        }
        else
        {
            builder.AddNpmApp(name, path, "start:build")
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

        return this;
    }

    public ProjectConfigurator AddWebDav()
    {
        var name = "asc-webDav";
        var path = "../ASC.WebDav/";
        var port = Constants.WebDavPort;
        
        if (isDocker)
        {
            var resourceBuilder = builder
                .AddDockerfile(name, path)
                .WithImageTag("dev")
                .WithEnvironment("log:name", "webDav")
                .WithHttpEndpoint(port, port, isProxied: false)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            AddBaseBind(resourceBuilder);
        }
        else
        {
            builder.AddNpmApp(name, path, "start:build")
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

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
}
