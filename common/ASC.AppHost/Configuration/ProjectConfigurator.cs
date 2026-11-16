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
    bool isDocker,
    bool isIntegrationTest = false)
{
    // The queues a test class has to wait on, by task type name (core:queue:<name>:maxThreadsCount). Single-thread
    // queues go to 8, the file operations, already at 10, to 20.
    private static readonly (string Queue, int Threads)[] _integrationTestQueueThreads =
    [
        ("AuditReportTask", 8),
        ("RoomIndexExportTask", 8),
        ("FormFillingReportTask", 8),
        ("CustomerOperationsReportTask", 8),
        ("CreateRoomTemplateOperation", 8),
        ("CreateRoomFromTemplateOperation", 8),
        ("MetadataCascadeOperation", 8),
        ("RemoveProgressItem", 8),
        ("ReassignProgressItem", 8),
        ("DeletePersonalFolderProgressItem", 8),
        ("UpdateUserTypeProgressItem", 8),
        ("FileDeleteOperation", 20),
        ("FileMoveCopyOperation", 20),
        ("FileDuplicateOperation", 20),
        ("FileDownloadOperation", 20),
        ("FileMarkAsReadOperation", 20),
        ("FileBuilderOperation", 20)
    ];

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
            AddProjectWithDefaultConfiguration<TProject>(projectPort);
        }

        return this;
    }

    private void AddProjectWithDefaultConfiguration<TProject>(int projectPort) where TProject : IProjectMetadata, new()
    {
        var project = builder
            .AddProject<TProject>(GetProjectName<TProject>())
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        // The testing builder randomizes every endpoint port, but the router's upstream map (NginxConfiguration)
        // and the document server reach the services at their fixed ports through host.docker.internal. With
        // random ports every request through 8092 failed: the editors could not fetch a docbuilder script, a
        // file to convert or a callback, so no document-server scenario could run in the tests.
        if (isIntegrationTest)
        {
            project.WithEndpoint("http", e =>
            {
                e.Port = e.TargetPort = projectPort;
                e.IsProxied = false;
            });
        }

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
            .WithEnvironment("ai:mcp:0:endpoint", new UriBuilder(Uri.UriSchemeHttp, "localhost", Constants.DocSpaceMcpPort, "mcp").Uri.ToString())
            .WithEnvironment("ai:mcpInternalHost", Constants.HostDockerInternal);

        ConfigureForwardedHeadersNetworks(project);

        // Map the dev HTTPS host to the default standalone tenant.
        project.WithEnvironment("CORE__LOCAL_ADDRESSES", $"{Constants.AppHostHttpsHost},{Constants.HostDockerInternal}");

        // Every launch profile points $STORAGE_ROOT/log__dir at <root>/Data and <root>/Logs, and the
        // integration tests reuse those very profiles. Redirect them into a `test` subfolder so a test
        // run stays out of the developer's own trees. `web:temp` goes there too — no launch profile
        // sets it, so services would otherwise write into <ContentRoot>/temp inside the sources.
        // Applied after AddProject, so it wins over the same keys coming from the launch profile.
        if (isIntegrationTest)
        {
            project.WithEnvironment("$STORAGE_ROOT", AppPaths.GetTestStorageRoot(basePath))
                .WithEnvironment("log__dir", AppPaths.GetTestLogsDirectory(basePath))
                .WithEnvironment("web:temp", AppPaths.GetTestTempDirectory(basePath))

                // Read by HostOptions.Initialize — the generic host's grace period for hosted services
                // to observe their stopping token. The default is 30 s, and ASC.Files.Worker reaches it
                // exactly: its ElasticSearch indexer is usually mid-IndexAll when the run ends, and that
                // loop does not observe the token, so every Files run paid a measured 31.3 s of teardown.
                // Nothing here needs a graceful stop — the portals and their storage are thrown away —
                // and an honest shutdown takes ~0.5 s, so this only trims the hang.
                .WithEnvironment("shutdownTimeoutSeconds", "3")

                // Small ceilings for unpacking migration backups, see Constants.MigrationTestMaxExtractedSize and MigrationTestMaxExtractedEntries.
                .WithEnvironment("migration:maxExtractedSize", Constants.MigrationTestMaxExtractedSize.ToString(CultureInfo.InvariantCulture))
                .WithEnvironment("migration:maxExtractedEntries", Constants.MigrationTestMaxExtractedEntries.ToString(CultureInfo.InvariantCulture))

                // Without an absolute authority IdentityClient calls {ServerRootPath}/api/2.0/oauth2/, the
                // portal's own host on the calling service's port: <alias>.docspace.localhost, which Windows
                // does not resolve, so deleting a user failed. Before the tests had a base domain that host was
                // localhost and the call reached the calling service itself, whose 404 was ignored, so the
                // identity service never saw it. The router sends /api/2.0/oauth2/clients to identity. 127.0.0.1, not
                // localhost: the router's port listens on IPv4 only and Windows would try ::1 first, ~2 s per call.
                .WithEnvironment("core:oidc:authority", new UriBuilder(Uri.UriSchemeHttp, IPAddress.Loopback.ToString(), Constants.RestyPort, "api/2.0/oauth2").Uri.ToString());

            // A queue runs as many of its tasks at once as its registration says, one unless stated. Fine for an
            // installation, but here every test class starts its own reports and operations in parallel, so a test
            // waited in the queue behind the tasks of the other classes and ran out of its deadline.
            foreach (var (queue, threads) in _integrationTestQueueThreads)
            {
                project.WithEnvironment($"core:queue:{queue}:maxThreadsCount", threads.ToString(CultureInfo.InvariantCulture));
            }
        }

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

        ApplyServiceName(project);
        ApplyCustomOtlpEndpoint(project);
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
            .WithEnvironment("ai:mcp:0:endpoint", new UriBuilder(Uri.UriSchemeHttp, Constants.DocSpaceMcpContainer, Constants.DocSpaceMcpPort, "mcp").Uri.ToString())
            .WithEnvironment("ai:mcpInternalHost", Constants.OpenRestyContainer)
            .WithArgs($"{dllPath}{name.Replace('_', '.')}.dll")
            .WithEntrypoint("dotnet")
            .WithWorkstationGc()
            .WithMemoryLimit(ContainerMemoryExtensions.DotNetServiceLimit);

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

        resourceBuilder.WithEnvironment("CORE__LOCAL_ADDRESSES", $"{Constants.AppHostHttpsHost},{Constants.HostDockerInternal}");

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
        ApplyServiceName(resourceBuilder);
        ApplyCustomOtlpEndpoint(resourceBuilder);
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
            ApplyServiceName(resourceBuilder);
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

            if (isIntegrationTest)
            {
                // Same idea as for the projects, but socket.io reads its own `logPath` key.
                resourceBuilder.WithEnvironment("logPath", AppPaths.GetTestLogsDirectory(basePath));
            }

            connectionManager.AddWaitFor(resourceBuilder);
            ApplyServiceName(resourceBuilder);
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
            ApplyServiceName(resourceBuilder);
        }
        else
        {
            var resourceBuilder = builder.AddJavaScriptApp(name, path, "start")
                .WithYarn()
                .WithEnvironment("NODE_ENV", "development")
                .WithEnvironment("API_HOST", $"http://localhost:{Constants.AppHostPort.ToString()}")
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            ApplyServiceName(resourceBuilder);
        }

        return this;
    }

    // Set by AddDocumentAgent; read lazily by AddAiChat's environment, so the order of the two does not matter.
    private string? _documentAgentUrl;
    private string? _documentAgentToken;

    /// <summary>
    /// The document agent (the sibling `document-agent` repository, its `agent/` service) next to the
    /// AI chat, without a sandbox: tasks run on the model and the request's MCP servers and answer in
    /// text, which is enough to exercise the chat's `document_agent` tool end to end. Skipped, with a
    /// note, when the sources are not there. `DOCUMENT_AGENT_PATH` names another checkout;
    /// `DOCUMENT_AGENT_SANDBOX_URL` and `DOCUMENT_AGENT_SANDBOX_TOKEN` hand it a real sandbox side.
    /// </summary>
    public ProjectConfigurator AddDocumentAgent()
    {
        var name = Constants.DocumentAgentContainer;
        var path = builder.Configuration["DOCUMENT_AGENT_PATH"]
                   ?? Path.GetFullPath(Path.Combine(basePath, "..", "document-agent", "agent"));
        var port = Constants.DocumentAgentPort;
        var token = builder.Configuration["DOCUMENT_AGENT_TOKEN"] ?? "dev-document-agent";
        var sandboxUrl = builder.Configuration["DOCUMENT_AGENT_SANDBOX_URL"] ?? "";
        var sandboxToken = builder.Configuration["DOCUMENT_AGENT_SANDBOX_TOKEN"] ?? "";

        if (!Directory.Exists(path))
        {
            Console.WriteLine($"document agent: no sources at {path}, not started (set DOCUMENT_AGENT_PATH)");
            return this;
        }

        if (isDocker)
        {
            var nodeVersion = File.ReadAllText(Path.Combine(path, ".node-version")).Trim();
            var resourceBuilder = builder
                .AddDockerfile(name, path)
                .WithImageTag("dev")
                .WithBuildArg("NODE_VERSION", nodeVersion)
                .WithEnvironment("AGENT_PUBLIC_URL", new UriBuilder(Uri.UriSchemeHttp, name, Constants.AgentContainerPort).ToString())
                .WithEnvironment("AGENT_AUTH_TOKEN", token)
                .WithEnvironment("AGENT_SANDBOX_URL", sandboxUrl)
                .WithEnvironment("AGENT_SANDBOX_TOKEN", sandboxToken)
                .WithEnvironment("AGENT_LOG_LEVEL", "info")
                .WithHttpEndpoint(port, Constants.AgentContainerPort, isProxied: false)
                .WithHttpHealthCheck("/healthz")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            ApplyServiceName(resourceBuilder);
            _documentAgentUrl = new UriBuilder(Uri.UriSchemeHttp, name, Constants.AgentContainerPort).ToString();
        }
        else
        {
            var resourceBuilder = builder.AddJavaScriptApp(name, path, "dev")
                .WithPnpm()
                .WithEnvironment("AGENT_PORT", port.ToString())
                .WithEnvironment("AGENT_PUBLIC_URL", $"http://localhost:{port.ToString()}")
                .WithEnvironment("AGENT_AUTH_TOKEN", token)
                .WithEnvironment("AGENT_SANDBOX_URL", sandboxUrl)
                .WithEnvironment("AGENT_SANDBOX_TOKEN", sandboxToken)
                .WithEnvironment("AGENT_LOG_LEVEL", "info")
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/healthz")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            ApplyServiceName(resourceBuilder);
            _documentAgentUrl = $"http://localhost:{port}";
        }

        _documentAgentToken = token;

        return this;
    }

    public ProjectConfigurator AddAiChat()
    {
        var name = "onlyoffice-aiChat";
        var path = Path.Combine("..", "ASC.AI.Chat");
        var port = Constants.AiChatPort;

        if (isDocker)
        {
            var resourceBuilder = builder
                .AddDockerfile(name, path)
                .WithImageTag("dev")
                .WithEnvironment("log:dir", "/logs")
                .WithEnvironment("log:name", "aiChat")
                .WithEnvironment("API_HOST", new UriBuilder(Uri.UriSchemeHttp, Constants.OpenRestyContainer, Constants.RestyPort).ToString())
                .WithEnvironment("AI_SERVICE_URL", new UriBuilder(Uri.UriSchemeHttp, GetProjectName<ASC_AI>(), Constants.AiPort).ToString())
                .WithEnvironment("app:appsettings", "/buildtools/config")
                // `__` form, not `:` — AI.Chat's nconf would otherwise nest a
                // `:`-keyed var and turn the `ai.mcp` array into an object.
                .WithEnvironment("AI__MCP__0__ENDPOINT", new UriBuilder(Uri.UriSchemeHttp, Constants.DocSpaceMcpContainer, Constants.DocSpaceMcpPort).ToString() + "mcp")
                // The document agent (see ASC.AI.Chat/README.md): the one AddDocumentAgent started, else
                // AI__DOCUMENT_AGENT__URL and AI__DOCUMENT_AGENT__TOKEN from the AppHost environment; empty leaves the tool out.
                .WithEnvironment("AI__DOCUMENT_AGENT__URL", () => _documentAgentUrl ?? builder.Configuration["AI:DOCUMENT_AGENT:URL"] ?? "")
                .WithEnvironment("AI__DOCUMENT_AGENT__TOKEN", () => _documentAgentToken ?? builder.Configuration["AI:DOCUMENT_AGENT:TOKEN"] ?? "")
                // The agent runs on the same network as the portal's MCP server, so it gets it too (see ASC.AI.Chat/README.md).
                .WithEnvironment("AI__DOCUMENT_AGENT__SHARE_SYSTEM_SERVERS", "true")
                .WithHttpEndpoint(port, port, isProxied: false)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            AddBaseBind(resourceBuilder);

            resourceBuilder.WithEnvironment("openTelemetry:enable", "true");
            resourceBuilder.WithOtlpExporter();
            ApplyServiceName(resourceBuilder);
            ApplyCustomOtlpEndpoint(resourceBuilder);
        }
        else
        {
            var resourceBuilder = builder.AddJavaScriptApp(name, path, "dev")
                .WithYarn()
                .WithEnvironment("NODE_ENV", "development")
                // `__` form, not `:` — AI.Chat's nconf would otherwise nest a
                // `:`-keyed var and turn the `ai.mcp` array into an object.
                .WithEnvironment("AI__MCP__0__ENDPOINT", new UriBuilder(Uri.UriSchemeHttp, "localhost", Constants.DocSpaceMcpPort) + "mcp")
                .WithEnvironment("AI__DOCUMENT_AGENT__URL", () => _documentAgentUrl ?? builder.Configuration["AI:DOCUMENT_AGENT:URL"] ?? "")
                .WithEnvironment("AI__DOCUMENT_AGENT__TOKEN", () => _documentAgentToken ?? builder.Configuration["AI:DOCUMENT_AGENT:TOKEN"] ?? "")
                // The agent runs on the same network as the portal's MCP server, so it gets it too (see ASC.AI.Chat/README.md).
                .WithEnvironment("AI__DOCUMENT_AGENT__SHARE_SYSTEM_SERVERS", "true")
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            if (isIntegrationTest)
            {
                // The AI tests stand in for the model provider with a WireMock in the test process, which a
                // profile reaches through a loopback baseUrl — refused by default as an SSRF target.
                resourceBuilder.WithEnvironment("AI__ALLOW_PRIVATE_BASE_URL", "true");
                // The SSRF and validation tests hand the service names that do not resolve (`example.invalid`).
                // Node resolves through getaddrinfo on the libuv threadpool, 4 threads by default, and on Windows
                // one such lookup holds a thread for ~11 s (the guard's 3 s cap returns early but does not free
                // it). Four of them at once queue every other lookup behind them, including `localhost` for each
                // call to the AI service and the router, and undici gives up after 10 s: random 500s in the run.
                resourceBuilder.WithEnvironment("UV_THREADPOOL_SIZE", "64");
            }

            resourceBuilder.WithEnvironment("openTelemetry:enable", "true");
            resourceBuilder.WithOtlpExporter();
            ApplyServiceName(resourceBuilder);
            ApplyCustomOtlpEndpoint(resourceBuilder);
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
            ApplyServiceName(resourceBuilder);
        }
        else
        {
            var resourceBuilder = builder.AddJavaScriptApp(name, path, "start")
                .WithYarn()
                .WithEnvironment("NODE_ENV", "development")
                .WithHttpEndpoint(targetPort: port)
                .WithHttpHealthCheck("/health")
                .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

            ApplyServiceName(resourceBuilder);
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
            // Spring boots long after the container reports Running; without this, anything waiting
            // for the resource to go healthy (the integration-test fixtures) starts too early.
            .WithHttpHealthCheck("/health")
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        connectionManager.AddIdentityEnv(registrationBuilder);
        ApplyServiceName(registrationBuilder);

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
            .WithHttpHealthCheck("/health")
            .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

        connectionManager.AddIdentityEnv(authorizationBuilder);
        ApplyServiceName(authorizationBuilder);

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

    private void ApplyCustomOtlpEndpoint<T>(IResourceBuilder<T> resourceBuilder) where T : IResourceWithEnvironment
    {
        var otlpEndpoint = builder.Configuration["OtlpEndpoint"];

        if (string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            return;
        }

        var otlpProtocol = builder.Configuration["OtlpProtocol"];

        resourceBuilder
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
            .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", string.IsNullOrWhiteSpace(otlpProtocol) ? "grpc" : otlpProtocol);
    }

    private static void ApplyServiceName<T>(IResourceBuilder<T> resourceBuilder) where T : IResourceWithEnvironment
    {
        resourceBuilder.WithEnvironment("OTEL_SERVICE_NAME", resourceBuilder.Resource.Name);
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
