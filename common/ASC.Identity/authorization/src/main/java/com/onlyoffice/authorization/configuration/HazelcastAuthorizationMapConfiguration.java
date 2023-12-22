/**
 *
 */
package com.onlyoffice.authorization.configuration;

import com.hazelcast.config.*;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import org.redisson.api.RedissonClient;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 *
 */
@Getter
@Setter
@Configuration
@RequiredArgsConstructor
@ConfigurationProperties(prefix = "spring.cloud.cache.authorization")
public class HazelcastAuthorizationMapConfiguration {
    public static final String AUTHORIZATIONS = "authorization";
    private final String AUTHORIZATIONS_CACHE_CONFIG_NAME = "authorizations_cache_config";

    private int mapSizeMB = 100;
    private int mapSecondsTTL = 50;
    private int mapSecondsIdle = 60 * 60;
    private int mapAsyncBackup = 1;
    private int mapBackup = 1;

    @Bean
    public MapConfig mapConfig(RedissonClient client) {
        EvictionConfig evictionConfig = new EvictionConfig()
                .setSize(mapSizeMB)
                .setMaxSizePolicy(MaxSizePolicy.USED_HEAP_SIZE)
                .setEvictionPolicy(EvictionPolicy.LFU);

        MapConfig mapConfig = new MapConfig(AUTHORIZATIONS)
                .setName(AUTHORIZATIONS_CACHE_CONFIG_NAME)
                .setTimeToLiveSeconds(mapSecondsTTL)
                .setMaxIdleSeconds(mapSecondsIdle)
                .setAsyncBackupCount(mapAsyncBackup)
                .setBackupCount(mapBackup)
                .setReadBackupData(false)
                .setEvictionConfig(evictionConfig)
                .setMetadataPolicy(MetadataPolicy.CREATE_ON_UPDATE);

        mapConfig.getMapStoreConfig()
                .setInitialLoadMode(MapStoreConfig.InitialLoadMode.EAGER);

        return mapConfig;
    }
}
