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

using ASC.AppHost.Extensions;

using Microsoft.Extensions.Hosting;

using ResourceBuilderExtensions = ASC.AppHost.Extensions.ResourceBuilderExtensions;

namespace ASC.AppHost.Configuration;

public class ProjectConfigurator(
    IDistributedApplicationBuilder builder,
    ConnectionStringManager connectionManager,
    string basePath,
    bool isDocker)
{
    public void AddProjectWithDefaultConfiguration<TProject>(bool includeHealthCheck = true) where TProject : IProjectMetadata, new()
    {
        var project = builder
            .AddProject<TProject>(ResourceBuilderExtensions.GetProjectName<TProject>())
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        if (int.TryParse(builder.Configuration["Replicas"], out var replicas) && replicas > 1)
        {
            project.WithReplicas(replicas);
        }
        else
        {
            project.WithEnvironment("core:hosting:singletonMode", true.ToString());
        }

        project.AddBaseConfig(connectionManager, isDocker, includeHealthCheck);
        connectionManager.AddWaitFor(project);
    }

    public void AddProjectDocker<TProject>(int projectPort, bool includeHealthCheck = true) where TProject : IProjectMetadata, new()
    {
        var projectMetadata = new TProject();
        var projectBasePath = Path.GetDirectoryName(projectMetadata.ProjectPath) ?? basePath;

        var name = typeof(TProject).Name;
        var resourceBuilder = builder.AddDockerfile(ResourceBuilderExtensions.GetProjectName<TProject>(), projectBasePath, stage: "base");

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
            .WithEnvironment("web:hub:internal", $"http://{Constants.SocketIoContainer}:{Constants.SocketIoPort.ToString()}")
            .WithEnvironment("core:hosting:singletonMode", true.ToString())
            .WithEnvironment("pathToConf", "/buildtools/config/")
            .WithArgs($"{dllPath}{name.Replace('_', '.')}.dll")
            .WithEntrypoint("dotnet");

        resourceBuilder.AddBaseBind(basePath);

        if (projectPort != 0)
        {
            resourceBuilder
                .WithEnvironment("ASPNETCORE_HTTP_PORTS", projectPort.ToString())
                .WithHttpEndpoint(projectPort, projectPort)
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

        resourceBuilder.AddBaseConfig(connectionManager, isDocker, includeHealthCheck);
        connectionManager.AddWaitFor(resourceBuilder);

        resourceBuilder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true");
        resourceBuilder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true");
        resourceBuilder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory");

        if (resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode && resourceBuilder.ApplicationBuilder.Environment.IsDevelopment())
        {
            resourceBuilder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION", "true");
            resourceBuilder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION", "true");
        }

        resourceBuilder.WithOtlpExporter();
    }

    public IResourceBuilder<ContainerResource> AddSocketIoDocker()
    {
        var resourceBuilder = builder
            .AddDockerfile(Constants.SocketIoContainer, "../ASC.Socket.IO/")
            .WithImageTag("dev")
            .WithEnvironment("log:name", "socketIO")
            .WithEnvironment("API_HOST", $"http://{Constants.OpenRestyContainer}:{Constants.RestyPort.ToString()}")
            .WithEnvironment("Redis:Hosts:0:Host", () => ConnectionStringManager.SubstituteLocalhost(connectionManager.RedisHost) ?? string.Empty)
            .WithEnvironment("Redis:Hosts:0:Port", () => connectionManager.RedisPort ?? string.Empty)
            .WithHttpEndpoint(Constants.SocketIoPort, Constants.SocketIoPort, isProxied: false)
            .WithHttpHealthCheck("/health")
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        resourceBuilder.AddBaseBind(basePath);
        return resourceBuilder;
    }

    public IResourceBuilder<ContainerResource> AddSsoAuthDocker()
    {
        var resourceBuilder = builder
            .AddDockerfile("asc-ssoAuth", "../ASC.SSoAuth/")
            .WithImageTag("dev")
            .WithEnvironment("log:name", "ssoAuth")
            .WithEnvironment("API_HOST", $"http://{Constants.OpenRestyContainer}:{Constants.RestyPort.ToString()}")
            .WithEnvironment("app:appsettings", "/buildtools/config")
            .WithHttpEndpoint(Constants.SsoAuthPort, Constants.SsoAuthPort, isProxied: false)
            .WithHttpHealthCheck("/health")
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        resourceBuilder.AddBaseBind(basePath);
        return resourceBuilder;
    }

    public IResourceBuilder<ContainerResource> AddWebDavDocker()
    {
        var resourceBuilder = builder
            .AddDockerfile("asc-webDav", "../ASC.WebDav/")
            .WithImageTag("dev")
            .WithEnvironment("log:name", "webDav")
            .WithHttpEndpoint(Constants.WebDavPort, Constants.WebDavPort, isProxied: false)
            .WithHttpHealthCheck("/health")
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        resourceBuilder.AddBaseBind(basePath);
        return resourceBuilder;
    }
}
