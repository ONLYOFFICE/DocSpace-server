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

namespace ASC.AppHost.Configuration;

public class ConnectionStringManager(IDistributedApplicationBuilder builder)
{
    private MySqlConnectionStringBuilder? MySqlConnectionStringBuilder { get; set; }
    public Uri? RabbitMqUri { get; private set; }
    
    //TODO:create record
    public string? RedisHost { get; private set; }
    public string? RedisPort { get; private set; }
    public string? RedisPassword { get; private set; }

    private IResourceBuilder<MySqlDatabaseResource>? MySqlResource { get; set; }
    private IResourceBuilder<RabbitMQServerResource>? RabbitMqResource { get; set; }
    private IResourceBuilder<RedisResource>? RedisResource { get; set; }
    private IResourceBuilder<ExecutableResource>? MigrateResource { get; set; }
    private IResourceBuilder<ContainerResource>? EditorResource { get; set; }
    private IResourceBuilder<MailDevResource>? MailResource { get; set; }

    public ConnectionStringManager AddMySql()
    {
        MySqlResource = builder
            .AddMySql("mysql")
            .WithLifetime(ContainerLifetime.Persistent)
            .AddDatabase("docspace");

        builder.Eventing.Subscribe(MySqlResource.Resource, async (ConnectionStringAvailableEvent _, CancellationToken ct) =>
        {
            var connectionString = await MySqlResource.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
            if (connectionString != null && MySqlConnectionStringBuilder == null)
            {
                MySqlConnectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
            }
        });
        
        var path = Path.GetFullPath(Path.Combine("..", "Tools", "ASC.Migration.Runner", "bin", "Debug", "ASC.Migration.Runner.exe"));
        
        MigrateResource = builder
            .AddExecutable("migrate", path, Path.GetDirectoryName(path) ?? "")
            .WithReference(MySqlResource)
            .WaitFor(MySqlResource);
        
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

    public ConnectionStringManager AddRedis()
    {
#pragma warning disable ASPIRECERTIFICATES001
        RedisResource = builder
            .AddRedis("cache")
            .WithPassword(null)
            .WithoutHttpsCertificate()
            .WithLifetime(ContainerLifetime.Persistent);
//.WithRedisInsight();
#pragma warning restore ASPIRECERTIFICATES001
        
        builder.Eventing.Subscribe(RedisResource.Resource, async (ConnectionStringAvailableEvent _, CancellationToken ct) =>
        {
            var connectionString = await RedisResource.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);
            if (connectionString != null)
            {
                var hostAndPassword = connectionString.Split(',');
                var splitted = hostAndPassword[0].Split(':');
                if (splitted.Length == 2)
                {
                    RedisHost = splitted[0];
                    RedisPort = splitted[1];
                }

                if (hostAndPassword.Length > 1)
                {
                    var splittedHostAndPassword = hostAndPassword[1].Split('=');
                    if (splittedHostAndPassword.Length == 2)
                    {
                        RedisPassword = splittedHostAndPassword[1];
                    }
                }
            }
        });

        return this;
    }

    public ConnectionStringManager AddEditors()
    {
        EditorResource = builder
            .AddContainer(Constants.EditorsContainer, "onlyoffice/documentserver", "latest")
            //TODO:get from config or set for the rest projects
            .WithEnvironment("JWT_ENABLED", "true")
            .WithEnvironment("JWT_SECRET", "secret")
            .WithEnvironment("JWT_HEADER", "AuthorizationJwt");

        return this;
    }
    
    public ConnectionStringManager AddMailDev()
    {
        var resource = new MailDevResource(Constants.MailDevContainer);

        MailResource = builder.AddResource(resource)
            .WithImage(MailDevContainerImageTags.Image)
            .WithImageRegistry(MailDevContainerImageTags.Registry)
            .WithImageTag(MailDevContainerImageTags.Tag)
            .WithHttpEndpoint(
                targetPort: 1080,
                //port: httpPort,
                name: MailDevResource.HttpEndpointName)
            .WithEndpoint(
                targetPort: 1025,
                //port: smtpPort,
                name: MailDevResource.SmtpEndpointName);
         
         return this;
    }
    
    public void AddWaitFor<T>(
        IResourceBuilder<T> resourceBuilder,
        bool includeMigrate = true,
        bool includeRabbitMq = true,
        bool includeRedis = true,
        bool includeEditors = true,
        bool includeMailDev = true)  where T : IResourceWithWaitSupport
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

        if (includeMailDev && MailResource != null)
        {
            resourceBuilder.WaitFor(MailResource);
        }
    }

    public void AddBaseConfig<T>(IResourceBuilder<T> resourceBuilder, bool isDocker, bool includeHealthCheck = true) where T : IResourceWithEnvironment, IResourceWithWaitSupport, IResourceWithEndpoints
    {
        if (includeHealthCheck)
        {
            resourceBuilder.WithHttpHealthCheck("/health");
        }

        resourceBuilder
            .WithEnvironment("openTelemetry:enable", "true")
            .WithEnvironment("files:docservice:url:portal", ConnectionStringManager.SubstituteLocalhost("http://localhost"))
            .WithEnvironment("files:docservice:url:public", "http://localhost/ds-vpath");

        if (MySqlResource != null)
        {
            resourceBuilder
                .WithReference(MySqlResource, "default:connectionString");
        }
        
        if (MailResource != null)
        {
            resourceBuilder
                .WithReference(MailResource);
        }

        if (isDocker)
        {
            resourceBuilder.WithEnvironment("files:docservice:url:internal", $"http://{Constants.EditorsContainer}");
        }

        resourceBuilder
            .WithEnvironment("RabbitMQ:Hostname", () => RabbitMqUri != null ? isDocker ? $"{SubstituteLocalhost(RabbitMqUri.Host)}" : RabbitMqUri.Host : "")
            .WithEnvironment("RabbitMQ:Port", () => RabbitMqUri != null ? $"{RabbitMqUri.Port}" : "")
            .WithEnvironment("RabbitMQ:UserName", () => RabbitMqUri != null ? $"{RabbitMqUri.UserInfo.Split(':')[0]}" : "")
            .WithEnvironment("RabbitMQ:Password", () => RabbitMqUri != null ? $"{RabbitMqUri.UserInfo.Split(':')[1]}" : "")
            .WithEnvironment("RabbitMQ:VirtualHost", () => RabbitMqUri != null ? $"{RabbitMqUri.PathAndQuery}" : "");

        resourceBuilder
            .WithEnvironment("Redis:Hosts:0:Host", () => (isDocker ? SubstituteLocalhost(RedisHost) : RedisHost) ?? string.Empty)
            .WithEnvironment("Redis:Hosts:0:Port", () => RedisPort ?? string.Empty);

        if (!string.IsNullOrEmpty(RedisPassword))
        {
            resourceBuilder.WithEnvironment("Redis:Password", () => RedisPassword ?? string.Empty);
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
            .WithEnvironment("REDIS_HOST", () => SubstituteLocalhost(RedisHost) ?? string.Empty)
            .WithEnvironment("REDIS_PORT", () => RedisPort ?? string.Empty);

        if (!string.IsNullOrEmpty(RedisPassword))
        {
            resourceBuilder.WithEnvironment("REDIS_PASSWORD", () => RedisPassword ?? string.Empty);
        }

        AddWaitFor(resourceBuilder, includeEditors: false);
    }
    
    public static string? SubstituteLocalhost(string? host) => host?.Replace("localhost", Constants.HostDockerInternal);
}

internal static class MailDevContainerImageTags
{
    internal const string Registry = "docker.io";

    internal const string Image = "maildev/maildev";

    internal const string Tag = "2.2.1";
}

public sealed class MailDevResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    // Constants used to refer to well known-endpoint names, this is specific
    // for each resource type. MailDev exposes an SMTP endpoint and a HTTP
    // endpoint.
    internal const string SmtpEndpointName = "smtp";
    internal const string HttpEndpointName = "http";

    // An EndpointReference is a core Aspire type used for keeping
    // track of endpoint details in expressions. Simple literal values cannot
    // be used because endpoints are not known until containers are launched.
    private EndpointReference? _smtpReference;

    public EndpointReference SmtpEndpoint =>
        _smtpReference ??= new(this, SmtpEndpointName);

    // Required property on IResourceWithConnectionString. Represents a connection
    // string that applications can use to access the MailDev server. In this case
    // the connection string is composed of the SmtpEndpoint endpoint reference.
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"smtp://{SmtpEndpoint.Property(EndpointProperty.HostAndPort)}"
        );
}