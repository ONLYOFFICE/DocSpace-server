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
    public MySqlConnectionStringBuilder? MySqlConnectionStringBuilder { get; private set; }
    public Uri? RabbitMqUri { get; private set; }
    
    //TODO:create record
    public string? RedisHost { get; private set; }
    public string? RedisPort { get; private set; }
    public string? RedisPassword { get; private set; }

    public IResourceBuilder<MySqlDatabaseResource>? MySqlResource { get; private set; }
    private IResourceBuilder<RabbitMQServerResource>? RabbitMqResource { get; set; }
    private IResourceBuilder<RedisResource>? RedisResource { get; set; }
    private IResourceBuilder<ExecutableResource>? MigrateResource { get; set; }
    private IResourceBuilder<ContainerResource>? EditorResource { get; set; }

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
        RedisResource = builder
            .AddRedis("cache")
            .WithPassword(null)
            .WithLifetime(ContainerLifetime.Persistent);
//.WithRedisInsight();

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
    
    public void AddWaitFor<T>(
        IResourceBuilder<T> resourceBuilder,
        bool includeMigrate = true,
        bool includeRabbitMq = true,
        bool includeRedis = true,
        bool includeEditors = true)  where T : IResourceWithWaitSupport
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
    }
    
    public static string? SubstituteLocalhost(string? host) => host?.Replace("localhost", Constants.HostDockerInternal);
}