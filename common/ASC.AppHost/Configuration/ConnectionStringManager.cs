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

using MySqlConnector;

namespace ASC.AppHost.Configuration;

public record RedisConfig(string Host, string Port, string? Password = null);

public class ConnectionStringManager(IDistributedApplicationBuilder builder, string basePath)
{
    private MySqlConnectionStringBuilder? MySqlConnectionStringBuilder { get; set; }
    public Uri? RabbitMqUri { get; private set; }
    public RedisConfig? Redis { get; private set; }

    private IResourceBuilder<MySqlDatabaseResource>? MySqlDatabaseResource { get; set; }
    private IResourceBuilder<RabbitMQServerResource>? RabbitMqResource { get; set; }
    private IResourceBuilder<RedisResource>? RedisResource { get; set; }
    private IResourceBuilder<ExecutableResource>? MigrateResource { get; set; }
    private IResourceBuilder<ContainerResource>? EditorResource { get; set; }
    private IResourceBuilder<MailPitContainerResource>? MailResource { get; set; }
    private IResourceBuilder<ContainerResource>? OpensearchResource { get; set; }
    
    private IResourceBuilder<ContainerResource>? McpResource { get; set; }
    private IResourceBuilder<ContainerResource>? KeycloakResource { get; set; }
    private IResourceBuilder<ContainerResource>? OpenProjectResource { get; set; }


    public ConnectionStringManager AddMySql(bool withDbGate = false)
    {
        var mysqlResourceBuilder = builder.AddMySql("mysql")
            .WithLifetime(ContainerLifetime.Persistent);

        if (withDbGate)
        {
            mysqlResourceBuilder = mysqlResourceBuilder.WithDbGate();
        }
        
        MySqlDatabaseResource = mysqlResourceBuilder.AddDatabase("docspace");

        builder.Eventing.Subscribe(MySqlDatabaseResource.Resource, async (ConnectionStringAvailableEvent _, CancellationToken ct) =>
        {
            var connectionString = await MySqlDatabaseResource.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
            if (connectionString != null && MySqlConnectionStringBuilder == null)
            {
                MySqlConnectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
            }
        });
        
        var executableName = OperatingSystem.IsWindows() ? "ASC.Migration.Runner.exe" : "ASC.Migration.Runner";
        var path = Path.GetFullPath(Path.Combine("..", "Tools", "ASC.Migration.Runner", "bin", "Debug", executableName));
        
        MigrateResource = builder
            .AddExecutable("migrate", path, Path.GetDirectoryName(path) ?? "")
            .WithReference(MySqlDatabaseResource)
            .WaitFor(MySqlDatabaseResource);
        
        var isStandalone = String.Compare(builder.Configuration["APP_HOSTING_STANDALONE"], "true", StringComparison.OrdinalIgnoreCase) == 0;

        if (isStandalone)
        {
            MigrateResource.WithEnvironment("standalone", "true");
        }
        else
        {
            MigrateResource.WithEnvironment("standalone", "");
        }
        
        return this;
    }

    public ConnectionStringManager AddRabbitMq()
    {
        RabbitMqResource = builder
            .AddRabbitMQ("messaging")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithManagementPlugin();

        builder.Eventing.Subscribe(RabbitMqResource.Resource, async (ConnectionStringAvailableEvent _, CancellationToken ct) =>
        {
            var connectionString = await RabbitMqResource.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
            if (connectionString != null && RabbitMqUri == null && Uri.IsWellFormedUriString(connectionString, UriKind.Absolute))
            {
                RabbitMqUri = new Uri(connectionString);
            }
        });

        return this;
    }

    public ConnectionStringManager AddRedis(bool withRedisInsight = false)
    {
#pragma warning disable ASPIRECERTIFICATES001
        RedisResource = builder
            .AddRedis("cache")
            .WithPassword(null)
            .WithoutHttpsCertificate()
            .WithLifetime(ContainerLifetime.Persistent);
#pragma warning restore ASPIRECERTIFICATES001

        if (withRedisInsight)
        {
            RedisResource = RedisResource.WithRedisInsight();
        }
        
        builder.Eventing.Subscribe(RedisResource.Resource, async (ConnectionStringAvailableEvent _, CancellationToken ct) =>
        {
            var connectionString = await RedisResource.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
            if (connectionString != null && Redis == null)
            {
                var hostAndPassword = connectionString.Split(',');
                var splitted = hostAndPassword[0].Split(':');
                if (splitted.Length == 2)
                {
                    string? password = null;
                    if (hostAndPassword.Length > 1)
                    {
                        var splittedHostAndPassword = hostAndPassword[1].Split('=');
                        if (splittedHostAndPassword.Length == 2)
                        {
                            password = splittedHostAndPassword[1];
                        }
                    }
                    Redis = new RedisConfig(splitted[0], splitted[1], password);
                }
            }
        });

        return this;
    }

    public ConnectionStringManager AddMcpServer(bool withMcpInspector = false)
    {
        McpResource = builder
            .AddContainer(Constants.DocSpaceMcpContainer, "onlyoffice/docspace-mcp")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithEnvironment("DOCSPACE_INTERNAL", "true")
            .WithEnvironment("DOCSPACE_TRANSPORT", "http")
            .WithEnvironment("DOCSPACE_HOST", "0.0.0.0")
            .WithEnvironment("DOCSPACE_PORT", Constants.DocSpaceMcpPort.ToString())
            .WithHttpEndpoint(Constants.DocSpaceMcpPort, Constants.DocSpaceMcpPort);

        if (withMcpInspector)
        {
            WithMcpInspector(McpResource);
        }
        
        AddWaitFor(McpResource);
        
        return this;
    }
    
    IResourceBuilder<McpInspectorResource> WithMcpInspector(IResourceBuilder<ContainerResource> mcp) => mcp
        .ApplicationBuilder.AddMcpInspector("mcp-inspector")
        .WithMcpServer(mcp)
        .WithParentRelationship(mcp)
        .WaitFor(mcp)
        .WithUrls(x =>
        {
            x.Urls[0].DisplayText = "Inspector";
        });

    
    public ConnectionStringManager AddEditors()
    {
        var image = "onlyoffice/documentserver";
        
        switch (builder.Configuration["APP_EDITION"])
        {
            case "enterprise":
                image = "onlyoffice/documentserver-ee";
                break;
            case "developer":
                image = "onlyoffice/documentserver-de";
                break;
            default:
                break;
        }

        EditorResource = builder
            .AddContainer(Constants.EditorsContainer, image, "latest")
            //TODO:get from config or set for the rest projects
            .WithEnvironment("JWT_ENABLED", "true")
            .WithEnvironment("JWT_SECRET", "secret")
            .WithEnvironment("JWT_HEADER", "AuthorizationJwt")
            .WithBindMount(Path.Combine(basePath, "Data"), "/var/www/onlyoffice/Data");
           

        return this;
    }

    public ConnectionStringManager AddOpensearch()
    {
        OpensearchResource = builder
            .AddContainer(Constants.OpensearchContainer, "opensearchproject/opensearch", "2")
            .WithHttpEndpoint(port: Constants.OpensearchPort, targetPort: Constants.OpensearchPort)
            .WithEnvironment("DISABLE_INSTALL_DEMO_CONFIG", "true")
            .WithEnvironment("plugins.security.disabled", "true")
            .WithEnvironment("discovery.type", "single-node")
            .WithEntrypoint("/bin/bash")
            .WithArgs("-c", "opensearch-plugin install ingest-attachment --batch && /usr/share/opensearch/opensearch-docker-entrypoint.sh");
        
        builder.AddContainer("opensearch-dashboard", "opensearchproject/opensearch-dashboards", "2")
            .WithHttpEndpoint(targetPort: 5601)
            .WithEnvironment("OPENSEARCH_HOSTS", $"http://{Constants.OpensearchContainer}:{Constants.OpensearchPort.ToString()}")
            .WithEnvironment("DISABLE_SECURITY_DASHBOARDS_PLUGIN", "true");

        return this;
    }

    public ConnectionStringManager AddMailPit()
    {
        MailResource = builder.AddMailPit("mailpit");

        return this;
    }

    public ConnectionStringManager AddKeycloak()
    {
        // Keycloak listens on port 8080 inside the container by default.
        // We map host port KeycloakPort (8180) → container port KeycloakContainerPort (8080)
        // so it doesn't collide with the Identity Authorization service (also on 8080 host-side).
        // KC_HOSTNAME forces Keycloak to embed the public URL in issued tokens (iss claim)
        // and in OIDC discovery metadata. Without this, tokens contain the internal Docker
        // address (onlyoffice-keycloak:8080) as issuer, which OpenProject cannot reach
        // from the browser and which mismatches the configured issuer on validation.
        // KC_HOSTNAME_STRICT=false + KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true allow the server
        // to still accept back-channel requests on its internal address.
        KeycloakResource = builder
            .AddContainer(Constants.KeycloakContainer, "quay.io/keycloak/keycloak", "26.1")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithArgs("start-dev", "--import-realm")
            .WithEnvironment("KEYCLOAK_ADMIN", Constants.KeycloakAdminUser)
            .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", Constants.KeycloakAdminPassword)
            .WithEnvironment("KC_HOSTNAME", $"http://localhost:{Constants.KeycloakPort.ToString()}")
            .WithEnvironment("KC_HOSTNAME_STRICT", "false")
            .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", "true")
            .WithBindMount(
                Path.Combine(basePath, "server", "common", "ASC.AppHost", "keycloak", "realms"),
                "/opt/keycloak/data/import")
            // port: host-side port, targetPort: container-side port
            .WithHttpEndpoint(port: Constants.KeycloakPort, targetPort: Constants.KeycloakContainerPort, name: "http");

        return this;
    }

    public ConnectionStringManager AddOpenProject()
    {
        if (KeycloakResource == null)
        {
            throw new InvalidOperationException("Keycloak must be added before OpenProject.");
        }

        // With KC_HOSTNAME=localhost:8180, Keycloak embeds the public URL in all tokens
        // and discovery metadata. All endpoints therefore use the public base URL.
        // From inside the OpenProject container, localhost is unreachable — use
        // host.docker.internal to reach Keycloak on the host's port 8180.
        var oidcIssuerPublic   = $"http://localhost:{Constants.KeycloakPort}/realms/{Constants.KeycloakRealm}";
        var oidcIssuerDockerHost = $"http://{KnownHostNames.DockerDesktopHostBridge}:{Constants.KeycloakPort}/realms/{Constants.KeycloakRealm}";

        // OpenProject 15 ConfigurationMapper contains a bug: it maps attribute_map.email
        // to the key "mapping_mail", but the Provider model column is "mapping_email".
        // Passing any attribute_map via env crashes the seeder with
        // "unknown attribute 'mapping_mail'". Workaround: omit attribute_map entirely.
        // Keycloak sends standard OIDC claims (email, given_name, family_name,
        // preferred_username) by default, which OpenProject picks up without explicit mapping.
        OpenProjectResource = builder
            .AddContainer(Constants.OpenProjectContainer, "openproject/openproject", "15")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithEnvironment("OPENPROJECT_HOST__NAME", $"localhost:{Constants.OpenProjectPort.ToString()}")
            .WithEnvironment("OPENPROJECT_HTTPS", "false")
            .WithEnvironment("OPENPROJECT_DEFAULT__LANGUAGE", "en")
            // Disable EE banners so OIDC SSO button is shown on the login page in Community Edition.
            // Without this, EnterpriseToken.show_banners? returns true and filtered_strategy?
            // blocks all providers except "developer".
            .WithEnvironment("OPENPROJECT_EE__MANAGER__VISIBLE", "false")
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_DISPLAY__NAME", "DocSpace (Keycloak)")
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_IDENTIFIER", "openproject")
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_SECRET", "openproject-secret")
            // issuer must match the public URL embedded in tokens by KC_HOSTNAME
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_ISSUER", oidcIssuerPublic)
            // Browser-facing endpoints → public URL (localhost:8180)
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_AUTHORIZATION__ENDPOINT",
                $"{oidcIssuerPublic}/protocol/openid-connect/auth")
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_END__SESSION__ENDPOINT",
                $"{oidcIssuerPublic}/protocol/openid-connect/logout")
            // Server-to-server endpoints → host.docker.internal:8180
            // (container→host→Keycloak; issuer in responses will be localhost:8180 which matches)
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_TOKEN__ENDPOINT",
                $"{oidcIssuerDockerHost}/protocol/openid-connect/token")
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_USERINFO__ENDPOINT",
                $"{oidcIssuerDockerHost}/protocol/openid-connect/userinfo")
            .WithEnvironment("OPENPROJECT_OPENID__CONNECT_DOCSPACE_JWKS__URI",
                $"{oidcIssuerDockerHost}/protocol/openid-connect/certs")
            .WithHttpEndpoint(port: Constants.OpenProjectPort, targetPort: 80, name: "http")
            .WaitFor(KeycloakResource);

        return this;
    }

    public void AddWaitFor<T>(
        IResourceBuilder<T> resourceBuilder,
        bool includeMigrate = true,
        bool includeRabbitMq = true,
        bool includeRedis = true,
        bool includeEditors = true,
        bool includeOpensearch = true,
        bool includeMailPit = true
        )  where T : IResourceWithWaitSupport
    {
        if (includeMigrate && MigrateResource != null)
        {
            resourceBuilder.WaitForCompletion(MigrateResource);
        }

        if (includeRabbitMq && RabbitMqResource != null)
        {
            resourceBuilder.WaitFor(RabbitMqResource);
        }

        if (includeRedis && RedisResource != null)
        {
            resourceBuilder.WaitFor(RedisResource);
        }

        if (includeEditors && EditorResource != null)
        {
            resourceBuilder.WaitFor(EditorResource);
        }

        if (includeOpensearch && OpensearchResource != null)
        {
            resourceBuilder.WaitFor(OpensearchResource);
        }
        if (includeMailPit && MailResource != null)
        {
            resourceBuilder.WaitFor(MailResource);
        }
    }

    public void AddBaseConfig<T>(IResourceBuilder<T> resourceBuilder, bool isDocker) where T : IResourceWithEnvironment, IResourceWithWaitSupport, IResourceWithEndpoints
    {
        resourceBuilder.WithHttpHealthCheck("/health");
       
        resourceBuilder
            .WithEnvironment("openTelemetry:enable", "true")
            .WithEnvironment("files:docservice:url:portal", SubstituteLocalhost("http://localhost") + ":" + Constants.AppHostPort)
            .WithEnvironment("files:docservice:url:public", $"http://localhost:{Constants.AppHostPort.ToString()}/ds-vpath");

        if (MySqlDatabaseResource != null)
        {
            resourceBuilder
                .WithReference(MySqlDatabaseResource, "default:connectionString");
        }
        
        if (MailResource != null)
        {
            resourceBuilder
                .WithReference(MailResource)
                .WithEnvironment("core:notify:postman", "mailpit");
            
        }

        if (isDocker)
        {
            resourceBuilder.WithEnvironment("files:docservice:url:internal", $"http://{Constants.EditorsContainer}");
        }

        if (RabbitMqResource != null)
        {
            resourceBuilder
                .WithEnvironment("RabbitMQ:Hostname", () => RabbitMqUri != null ? isDocker ? $"{SubstituteLocalhost(RabbitMqUri.Host)}" : RabbitMqUri.Host : "")
                .WithEnvironment("RabbitMQ:Port", () => RabbitMqUri != null ? $"{RabbitMqUri.Port}" : "")
                .WithEnvironment("RabbitMQ:UserName", () => RabbitMqUri != null ? $"{RabbitMqUri.UserInfo.Split(':')[0]}" : "")
                .WithEnvironment("RabbitMQ:Password", () => RabbitMqUri != null ? $"{RabbitMqUri.UserInfo.Split(':')[1]}" : "")
                .WithEnvironment("RabbitMQ:VirtualHost", () => RabbitMqUri != null ? $"{RabbitMqUri.PathAndQuery}" : "");
        }
      
        if (RedisResource != null)
        {
            resourceBuilder
                .WithEnvironment("Redis:Hosts:0:Host", () => (isDocker ? SubstituteLocalhost(Redis?.Host) : Redis?.Host) ?? string.Empty)
                .WithEnvironment("Redis:Hosts:0:Port", () => Redis?.Port ?? string.Empty);

            if (!string.IsNullOrEmpty(Redis?.Password))
            {
                resourceBuilder.WithEnvironment("Redis:Password", () => Redis?.Password ?? string.Empty);
            }
        }

        if (OpensearchResource != null)
        {
            resourceBuilder
                .WithEnvironment("elastic:Scheme", () => "http")
                .WithEnvironment("elastic:Host", () => (isDocker ? Constants.OpensearchContainer : "localhost"))
                .WithEnvironment("elastic:Port", () => Constants.OpensearchPort.ToString())
                .WithEnvironment("elastic:Threads", () => "1");
        }

        if (KeycloakResource != null)
        {
            // KC_HOSTNAME makes Keycloak use localhost:8180 as its public URL in all tokens.
            // DocSpace services reach Keycloak via host.docker.internal:8180 (Docker) or
            // localhost:8180 (local dev). The issuer in tokens is always localhost:8180.
            var keycloakUrl = isDocker
                ? $"http://{KnownHostNames.DockerDesktopHostBridge}:{Constants.KeycloakPort}"
                : $"http://localhost:{Constants.KeycloakPort}";
            resourceBuilder
                .WithEnvironment("sso:keycloak:url", keycloakUrl)
                .WithEnvironment("sso:keycloak:realm", Constants.KeycloakRealm)
                .WithEnvironment("sso:keycloak:clientId", "docspace");
        }
    }
    
    public void AddIdentityEnv(IResourceBuilder<ContainerResource>  resourceBuilder)
    {
        resourceBuilder
            .WithEnvironment("JDBC_URL", () => MySqlConnectionStringBuilder != null ? $"{SubstituteLocalhost(MySqlConnectionStringBuilder.Server)}:{MySqlConnectionStringBuilder.Port}" : string.Empty)
            .WithEnvironment("JDBC_DATABASE", () => MySqlConnectionStringBuilder?.Database ?? string.Empty)
            .WithEnvironment("JDBC_USER_NAME", () => MySqlConnectionStringBuilder?.UserID ?? string.Empty)
            .WithEnvironment("JDBC_PASSWORD", () => MySqlConnectionStringBuilder?.Password ?? string.Empty);

        resourceBuilder
            .WithEnvironment("RABBIT_HOST", () => RabbitMqUri != null ? $"{SubstituteLocalhost(RabbitMqUri.Host)}" : string.Empty)
            .WithEnvironment("RABBIT_URI", () => RabbitMqUri != null ? $"{SubstituteLocalhost(RabbitMqUri.ToString())}" : string.Empty);

        resourceBuilder
            .WithEnvironment("REDIS_HOST", () => SubstituteLocalhost(Redis?.Host) ?? string.Empty)
            .WithEnvironment("REDIS_PORT", () => Redis?.Port ?? string.Empty);

        if (!string.IsNullOrEmpty(Redis?.Password))
        {
            resourceBuilder.WithEnvironment("REDIS_PASSWORD", () => Redis?.Password ?? string.Empty);
        }

        AddWaitFor(resourceBuilder, includeEditors: false);
    }
    
    public static string? SubstituteLocalhost(string? host) => host?.Replace(KnownHostNames.Localhost, KnownHostNames.DockerDesktopHostBridge);
}