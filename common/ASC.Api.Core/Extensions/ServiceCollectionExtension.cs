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

namespace ASC.Api.Core.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddCacheNotify(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfiguration = configuration.GetSection("Redis").Get<RedisConfiguration>();
        var kafkaConfiguration = configuration.GetSection("kafka").Get<KafkaSettings>();
        var rabbitMqConfiguration = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>();

        if (redisConfiguration != null)
        {
            services.AddStackExchangeRedisExtensions<RedisProtobufSerializer>(serviceProvider => new List<RedisConfiguration> { serviceProvider.GetRequiredService<RedisConfiguration>() });

            services.AddSingleton(typeof(ICacheNotify<>), typeof(RedisCacheNotify<>));
        }
        else if (rabbitMqConfiguration != null)
        {
            services.AddSingleton(typeof(ICacheNotify<>), typeof(RabbitMQCache<>));
        }
        else if (kafkaConfiguration != null && !string.IsNullOrEmpty(kafkaConfiguration.BootstrapServers))
        {
            services.AddSingleton(typeof(ICacheNotify<>), typeof(KafkaCacheNotify<>));
        }
        else
        {
            services.AddSingleton(typeof(ICacheNotify<>), typeof(MemoryCacheNotify<>));
        }

        return services;
    }

    public static IServiceCollection AddDistributedCache(this IServiceCollection services, IConnectionMultiplexer connection)
    {        
        if (connection != null)
        {
            services.AddStackExchangeRedisCache(config =>
            {
                config.ConnectionMultiplexerFactory = () => Task.FromResult(connection);
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }

    public static IServiceCollection AddDistributedLock(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfiguration = configuration.GetSection("Redis").Get<RedisConfiguration>();

        if (redisConfiguration != null)
        {            
            services.AddSingleton<Medallion.Threading.IDistributedLockProvider>(sp =>
            {
                var database = sp.GetRequiredService<IRedisClient>().GetDefaultDatabase().Database;
                var cfg = sp.GetRequiredService<IConfiguration>();

                return new RedisDistributedSynchronizationProvider(database, opt =>
                {
                    if (TimeSpan.TryParse(cfg["core:lock:expiry"], out var expiry))
                    {
                        opt.Expiry(expiry);
                    }

                    if (TimeSpan.TryParse(cfg["core:lock:extendInterval"], out var extendInterval))
                    {
                        opt.ExtensionCadence(extendInterval);
                    }
                    
                    if (TimeSpan.TryParse(cfg["core:lock:minValidityTime"], out var minValidityTime))
                    {
                        opt.MinValidityTime(minValidityTime);
                    }

                    if (TimeSpan.TryParse(cfg["core:lock:minSleepTime"], out var minSleepTime)
                        && TimeSpan.TryParse(cfg["core:lock:maxSleepTime"], out var maxSleepTime))
                    {
                        opt.BusyWaitSleepTime(minSleepTime, maxSleepTime);
                    }
                });
            });

            return services.AddSingleton<IDistributedLockProvider, RedisLockProvider>(sp =>
            {
                var redisClient = sp.GetRequiredService<IRedisClient>();
                var logger = sp.GetRequiredService<ILogger<RedisLockProvider>>();
                var cfg = sp.GetRequiredService<IConfiguration>();
                var internalProvider = sp.GetRequiredService<Medallion.Threading.IDistributedLockProvider>();
                
                return new RedisLockProvider(redisClient, logger, internalProvider, opt =>
                {
                    if (TimeSpan.TryParse(cfg["core:lock:expiry"], out var expiry))
                    {
                        opt.Expiry(expiry);
                    }

                    if (TimeSpan.TryParse(cfg["core:lock:extendInterval"], out var extendInterval))
                    {
                        opt.ExtendInterval(extendInterval);
                    }

                    if (TimeSpan.TryParse(cfg["core:lock:minTimeout"], out var minTimeout))
                    {
                        opt.MinTimeout(minTimeout);
                    }
                });
            });
        }

        var zooKeeperConfiguration = configuration.GetSection("Zookeeper").Get<ZooKeeperConfiguration>();

        if (zooKeeperConfiguration != null)
        {
            services.AddSingleton<Medallion.Threading.IDistributedLockProvider>(_ =>
            {
                return new ZooKeeperDistributedSynchronizationProvider(new ZooKeeperPath(zooKeeperConfiguration.DirectoryPath), zooKeeperConfiguration.Connection,
                    options =>
                    {
                        if (zooKeeperConfiguration.ConnectionTimeout.HasValue)
                        {
                            options.ConnectTimeout(zooKeeperConfiguration.ConnectionTimeout.Value);
                        }

                        if (zooKeeperConfiguration.SessionTimeout.HasValue)
                        {
                            options.SessionTimeout(zooKeeperConfiguration.SessionTimeout.Value);
                        }
                    });
            });

            return services.AddSingleton<IDistributedLockProvider, ZooKeeperDistributedLockProvider>(sp =>
            {
                var internalProvider = sp.GetRequiredService<Medallion.Threading.IDistributedLockProvider>();
                var logger = sp.GetRequiredService<ILogger<ZooKeeperDistributedLockProvider>>();
                var cfg = sp.GetRequiredService<IConfiguration>();
                
                return TimeSpan.TryParse(cfg["core:lock:minTimeout"], out var minTimeout) 
                    ? new ZooKeeperDistributedLockProvider(internalProvider, logger, minTimeout) 
                    : new ZooKeeperDistributedLockProvider(internalProvider, logger);
            });
        }

        throw new NotImplementedException("DistributedLock: Provider not found.");
    }

    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

        var rabbitMqConfiguration = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>();
        var activeMqConfiguration = configuration.GetSection("ActiveMQ").Get<ActiveMQSettings>();

        if (rabbitMqConfiguration != null)
        {
            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();

                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();

                var connectionFactory = rabbitMqConfiguration.GetConnectionFactory();

                var retryCount = 5;

                if (!string.IsNullOrEmpty(cfg["core:eventBus:connectRetryCount"]))
                {
                    retryCount = int.Parse(cfg["core:eventBus:connectRetryCount"]);
                }

                return new DefaultRabbitMQPersistentConnection(connectionFactory, logger, retryCount);
            });

            services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();

                var rabbitMqPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ>>();
                var eventBusSubscriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                var serializer = new ProtobufSerializer();

                var subscriptionClientName = "asc_event_bus_default_queue";

                if (!string.IsNullOrEmpty(cfg["core:eventBus:subscriptionClientName"]))
                {
                    subscriptionClientName = cfg["core:eventBus:subscriptionClientName"];
                }

                var retryCount = 5;

                if (!string.IsNullOrEmpty(cfg["core:eventBus:connectRetryCount"]))
                {
                    retryCount = int.Parse(cfg["core:eventBus:connectRetryCount"]);
                }

                return new EventBusRabbitMQ(rabbitMqPersistentConnection, logger, iLifetimeScope, eventBusSubscriptionsManager, serializer, subscriptionClientName, retryCount);
            });
        }
        else if (activeMqConfiguration != null)
        {
            services.AddSingleton<IActiveMQPersistentConnection>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();

                var logger = sp.GetRequiredService<ILogger<DefaultActiveMQPersistentConnection>>();

                var factory = new NMSConnectionFactory(activeMqConfiguration.Uri);

                var retryCount = 5;

                if (!string.IsNullOrEmpty(cfg["core:eventBus:connectRetryCount"]))
                {
                    retryCount = int.Parse(cfg["core:eventBus:connectRetryCount"]);
                }

                return new DefaultActiveMQPersistentConnection(factory, logger, retryCount);
            });

            services.AddSingleton<IEventBus, EventBusActiveMQ>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();

                var activeMqPersistentConnection = sp.GetRequiredService<IActiveMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusActiveMQ>>();
                var eventBusSubscriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                var serializer = new ProtobufSerializer();

                var subscriptionClientName = "asc_event_bus_default_queue";

                if (!string.IsNullOrEmpty(cfg["core:eventBus:subscriptionClientName"]))
                {
                    subscriptionClientName = cfg["core:eventBus:subscriptionClientName"];
                }

                var retryCount = 5;

                if (!string.IsNullOrEmpty(cfg["core:eventBus:connectRetryCount"]))
                {
                    retryCount = int.Parse(cfg["core:eventBus:connectRetryCount"]);
                }

                return new EventBusActiveMQ(activeMqPersistentConnection, logger, iLifetimeScope, eventBusSubscriptionsManager, serializer, subscriptionClientName, retryCount);
            });
        }
        else
        {
            throw new NotImplementedException("EventBus: Provider not found.");
        }

        return services;
    }


    private static readonly List<string> _registeredActivePassiveHostedService = [];
    private static readonly object _locker = new();

    /// <remarks>
    /// Add a IHostedService for given type. 
    /// Only one copy of this instance type will active in multi process architecture.
    /// </remarks>
    public static void AddActivePassiveHostedService<T>(this IServiceCollection services, DIHelper diHelper, 
                                                                                          IConfiguration configuration,
                                                                                          string workerTypeName = null) where T : ActivePassiveBackgroundService<T>
    {
        var typeName = workerTypeName ?? typeof(T).GetFormattedName();

        lock (_locker)
        {
            if (_registeredActivePassiveHostedService.Contains(typeName))
            {
                throw new Exception($"Service with name '{typeName}' already registered. Please, rename service name");
            }

            _registeredActivePassiveHostedService.Add(typeName);
        }

        diHelper.TryAdd<IRegisterInstanceDao<T>, RegisterInstanceDao<T>>();
        diHelper.TryAdd<IRegisterInstanceManager<T>, RegisterInstanceManager<T>>();
        services.AddHostedService<RegisterInstanceWorkerService<T>>();
        services.Configure<InstanceWorkerOptions<T>>(x =>
        {
            configuration.GetSection("core:hosting").Bind(x);
            x.WorkerTypeName = workerTypeName ?? typeof(T).GetFormattedName();
        });
        
        services.AddHostedService<T>();
    }

    public static IServiceCollection AddDistributedTaskQueue(this IServiceCollection services)
    {
        services.AddTransient<DistributedTaskQueue>();

        services.AddSingleton<IDistributedTaskQueueFactory, DefaultDistributedTaskQueueFactory>();

        return services;
    }

    public static IServiceCollection AddStartupTask<T>(this IServiceCollection services)
                                    where T : class, IStartupTask
    {
        services.AddTransient<IStartupTask, T>();

        return services;
    }

    public static async Task<IConnectionMultiplexer> GetRedisConnectionMultiplexerAsync(this IServiceCollection services, IConfiguration configuration, string clientName)
    {
        var redisConfiguration = configuration.GetSection("Redis").Get<RedisConfiguration>();

        if (redisConfiguration == null)
        {
            return null;
        }

        var configurationOption = redisConfiguration.ConfigurationOptions;

        configurationOption.ClientName = clientName;

        var redisConnection = await RedisPersistentConnection.InitializeAsync(configurationOption);

        services
            .AddSingleton(redisConfiguration)
            .AddSingleton(redisConnection);

        return redisConnection.GetConnection();

    }
}
