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

namespace ASC.AppHost.Extensions;

public static class ResourceBuilderExtensions
{
    public static string GetProjectName<TProject>() where TProject : IProjectMetadata, new() =>
        typeof(TProject).Name.ToLower().Replace('_', '-');

    extension<T>(IResourceBuilder<T> resourceBuilder) where T : ContainerResource
    {
        public void AddBaseBind(string basePath)
        {
            resourceBuilder
                .WithBindMount(Path.Combine(basePath, "buildtools"), "/buildtools")
                .WithBindMount(Path.Combine(basePath, "Data"), "/data")
                .WithBindMount(Path.Combine(basePath, "Logs"), "/logs")
                .WithEnvironment("log:dir", "/logs");
        }

        public void AddIdentityEnv(ConnectionStringManager connectionManager)
        {
            resourceBuilder
                .WithEnvironment("JDBC_URL", () => connectionManager.MySqlConnectionStringBuilder != null ? 
                    $"{ConnectionStringManager.SubstituteLocalhost(connectionManager.MySqlConnectionStringBuilder.Server)}:{connectionManager.MySqlConnectionStringBuilder.Port}" : string.Empty)
                .WithEnvironment("JDBC_DATABASE", () => connectionManager.MySqlConnectionStringBuilder?.Database ?? string.Empty)
                .WithEnvironment("JDBC_USER_NAME", () => connectionManager.MySqlConnectionStringBuilder?.UserID ?? string.Empty)
                .WithEnvironment("JDBC_PASSWORD", () => connectionManager.MySqlConnectionStringBuilder?.Password ?? string.Empty);

            resourceBuilder
                .WithEnvironment("RABBIT_HOST", () => connectionManager.RabbitMqUri != null ? $"{ConnectionStringManager.SubstituteLocalhost(connectionManager.RabbitMqUri.Host)}" : string.Empty)
                .WithEnvironment("RABBIT_URI", () => connectionManager.RabbitMqUri != null ? $"{ConnectionStringManager.SubstituteLocalhost(connectionManager.RabbitMqUri.ToString())}" : string.Empty);

            resourceBuilder
                .WithEnvironment("REDIS_HOST", () => ConnectionStringManager.SubstituteLocalhost(connectionManager.RedisHost) ?? string.Empty)
                .WithEnvironment("REDIS_PORT", () => connectionManager.RedisPort ?? string.Empty);

            if (!string.IsNullOrEmpty(connectionManager.RedisPassword))
            {
                resourceBuilder.WithEnvironment("REDIS_PASSWORD", () => connectionManager.RedisPassword ?? string.Empty);
            }

            connectionManager.AddWaitFor(resourceBuilder, includeEditors: false);
        }
    }

    extension<T>(IResourceBuilder<T> resourceBuilder) where T : IResourceWithEnvironment, IResourceWithWaitSupport, IResourceWithEndpoints
    {
        public void AddBaseConfig(
            ConnectionStringManager connectionManager,
            bool isDocker,
            bool includeHealthCheck = true)
        {
            if (includeHealthCheck)
            {
                resourceBuilder.WithHttpHealthCheck("/health");
            }

            if (connectionManager.MySqlResource != null)
            {
                resourceBuilder
                    .WithEnvironment("openTelemetry:enable", "true")
                    .WithEnvironment("files:docservice:url:portal", ConnectionStringManager.SubstituteLocalhost("http://localhost"))
                    .WithEnvironment("files:docservice:url:public", "http://localhost/ds-vpath")
                    .WithReference(connectionManager.MySqlResource, "default:connectionString");
            }

            if (isDocker)
            {
                resourceBuilder.WithEnvironment("files:docservice:url:internal", $"http://{Constants.EditorsContainer}");
            }

            resourceBuilder
                .WithEnvironment("RabbitMQ:Hostname", () => connectionManager.RabbitMqUri != null ? 
                    isDocker ? $"{ConnectionStringManager.SubstituteLocalhost(connectionManager.RabbitMqUri.Host)}" : connectionManager.RabbitMqUri.Host : "")
                .WithEnvironment("RabbitMQ:Port", () => connectionManager.RabbitMqUri != null ? $"{connectionManager.RabbitMqUri.Port}" : "")
                .WithEnvironment("RabbitMQ:UserName", () => connectionManager.RabbitMqUri != null ? $"{connectionManager.RabbitMqUri.UserInfo.Split(':')[0]}" : "")
                .WithEnvironment("RabbitMQ:Password", () => connectionManager.RabbitMqUri != null ? $"{connectionManager.RabbitMqUri.UserInfo.Split(':')[1]}" : "")
                .WithEnvironment("RabbitMQ:VirtualHost", () => connectionManager.RabbitMqUri != null ? $"{connectionManager.RabbitMqUri.PathAndQuery}" : "");

            resourceBuilder
                .WithEnvironment("Redis:Hosts:0:Host", () => (isDocker ? ConnectionStringManager.SubstituteLocalhost(connectionManager.RedisHost) : connectionManager.RedisHost) ?? string.Empty)
                .WithEnvironment("Redis:Hosts:0:Port", () => connectionManager.RedisPort ?? string.Empty);

            if (!string.IsNullOrEmpty(connectionManager.RedisPassword))
            {
                resourceBuilder.WithEnvironment("Redis:Password", () => connectionManager.RedisPassword ?? string.Empty);
            }
        }
    }
}
