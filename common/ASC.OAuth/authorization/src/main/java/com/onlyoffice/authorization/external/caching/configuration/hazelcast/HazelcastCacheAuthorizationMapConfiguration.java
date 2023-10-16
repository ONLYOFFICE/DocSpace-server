/**
 *
 */
package com.onlyoffice.authorization.external.caching.configuration.hazelcast;

import com.hazelcast.config.*;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 *
 */
@Configuration
@ConfigurationProperties(prefix = "spring.application.cache.authorization")
@RequiredArgsConstructor
@Getter
@Setter
public class HazelcastCacheAuthorizationMapConfiguration {
    public static final String AUTHORIZATIONS = "authorization";
    private int mapSizeMB = 100;
    private int mapSecondsTTL = 50;
    private int mapSecondsIdle = 60 * 60;
    private int mapAsyncBackup = 1;
    private int mapBackup = 1;

    @Bean
    public MapConfig mapConfig() {
        EvictionConfig evictionConfig = new EvictionConfig()
                .setSize(mapSizeMB)
                .setMaxSizePolicy(MaxSizePolicy.USED_HEAP_SIZE)
                .setEvictionPolicy(EvictionPolicy.LFU);

        MapConfig mapConfig = new MapConfig(AUTHORIZATIONS)
                .setName("Cache Authorizations Config")
                .setTimeToLiveSeconds(mapSecondsTTL)
                .setMaxIdleSeconds(mapSecondsIdle)
                .setAsyncBackupCount(mapAsyncBackup)
                .setBackupCount(mapBackup)
                .setReadBackupData(false)
                .setEvictionConfig(evictionConfig)
                .setMetadataPolicy(MetadataPolicy.CREATE_ON_UPDATE);
        mapConfig.getMapStoreConfig().setInitialLoadMode(MapStoreConfig.InitialLoadMode.EAGER);
        return mapConfig;
    }
}
