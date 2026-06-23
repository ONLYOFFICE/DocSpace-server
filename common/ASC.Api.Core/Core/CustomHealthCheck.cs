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

namespace ASC.Api.Core.Core;

public static class CustomHealthCheck
{
    public static bool Running { get; set; }

    static CustomHealthCheck()
    {
        Running = true;
    }

    public static IServiceCollection AddCustomHealthCheck(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<WarmupState>();

        var hcBuilder = services.AddHealthChecks();

        hcBuilder.AddCheck("self", () => Running ? HealthCheckResult.Healthy()
                                    : HealthCheckResult.Unhealthy())
                 .AddCheck<WarmupHealthCheck>("warmup", tags: ["warmup", "services"])
                 .AddDatabase(configuration)
                 .AddDistibutedCache(configuration);
        //.AddMessageQueue(configuration);

        return services;
    }

    extension(IHealthChecksBuilder hcBuilder)
    {
        public IHealthChecksBuilder AddDistibutedCache(IConfiguration configuration)
        {
            var redisEnabled = !string.Equals(configuration["Redis:Enabled"], "false", StringComparison.OrdinalIgnoreCase);
            var redisConfiguration = configuration.GetSection("Redis").Get<RedisConfiguration>();

            if (redisConfiguration != null && redisEnabled)
            {
                hcBuilder.AddRedis(x => x.GetRequiredService<RedisPersistentConnection>().GetConnection(),
                    name: "redis",
                    tags: ["redis", "services"],
                    timeout: new TimeSpan(0, 0, 15));
            }

            return hcBuilder;
        }

        public IHealthChecksBuilder AddDatabase(IConfiguration configuration)
        {
            var configurationExtension = new ConfigurationExtension(configuration);

            var connectionString = configurationExtension.GetConnectionStrings("default");

            if (string.Equals(connectionString.ProviderName, "MySql.Data.MySqlClient"))
            {
                hcBuilder.AddMySql(connectionString.ConnectionString,
                    healthQuery: "SELECT 1;",
                    name: "mysqldb",
                    tags: ["mysqldb", "services"],
                    timeout: new TimeSpan(0, 0, 30));
            }
            else if (string.Equals(connectionString.ProviderName, "Npgsql"))
            {
                hcBuilder.AddNpgSql(connectionString.ConnectionString,
                    name: "postgredb",
                    tags: ["postgredb", "services"],
                    timeout: new TimeSpan(0, 0, 30));
            }

            return hcBuilder;
        }

        public IHealthChecksBuilder AddMessageQueue(IConfiguration configuration)
        {
            var rabbitMqEnabled = !string.Equals(configuration["RabbitMQ:Enabled"], "false", StringComparison.OrdinalIgnoreCase);
            var rabbitMQConfiguration = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>();

            if (rabbitMQConfiguration != null && rabbitMqEnabled)
            {
                hcBuilder.AddRabbitMQ(async sp =>
                    {
                        var rabbitMqPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();

                        return await rabbitMqPersistentConnection.GetConnection();
                    },
                    name: "rabbitMQ",
                    tags: ["rabbitMQ", "services"],
                    timeout: new TimeSpan(0, 0, 30));
            }
            else
            {
                var kafkaSettings = configuration.GetSection("kafka").Get<KafkaSettings>();

                if (kafkaSettings != null && !string.IsNullOrEmpty(kafkaSettings.BootstrapServers))
                {
                    var clientConfig = new ClientConfig { BootstrapServers = kafkaSettings.BootstrapServers };

                    hcBuilder.AddKafka(new ProducerConfig(clientConfig),
                        name: "kafka",
                        tags: ["kafka", "services"],
                        timeout: new TimeSpan(0, 0, 15)
                    );

                }
            }

            return hcBuilder;
        }
    }
}