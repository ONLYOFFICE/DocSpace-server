package com.onlyoffice.authorization.api.configuration;

import com.github.benmanes.caffeine.cache.Caffeine;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.cache.caffeine.CaffeineCacheManager;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;

import java.util.concurrent.TimeUnit;

/**
 *
 */
@Slf4j
@Configuration
@EnableCaching
public class CachingConfiguration {
    private final int CACHE_EXPIRATION = 3;
    private final int CACHE_SIZE = 300;
    private final TimeUnit CACHE_EXPIRATION_UNIT = TimeUnit.MINUTES;

    /**
     *
     * @return
     */
    @Bean
    public Caffeine caffeineConfig() {
        MDC.put("expiration", String.valueOf(CACHE_EXPIRATION));
        MDC.put("expirationUnit", CACHE_EXPIRATION_UNIT.name());
        MDC.put("size", String.valueOf(CACHE_SIZE));
        log.info("Building an in-memory cache");
        MDC.clear();

        return Caffeine.newBuilder().expireAfterWrite(CACHE_EXPIRATION, CACHE_EXPIRATION_UNIT)
                .maximumSize(CACHE_SIZE);
    }

    /**
     *
     * @param caffeine
     * @return
     */
    @Bean
    @Primary
    public CacheManager cacheManager(Caffeine caffeine) {
        var caffeineCacheManager = new CaffeineCacheManager();
        caffeineCacheManager.setCaffeine(caffeine);
        return caffeineCacheManager;
    }

    /**
     *
     * @return
     */
    @Bean("ascClientCacheManager")
    public CacheManager ascClientCacheManager() {
        var caffeineCacheManager = new CaffeineCacheManager();
        caffeineCacheManager.setCaffeine(Caffeine.newBuilder().expireAfterWrite(60, TimeUnit.SECONDS)
                .maximumSize(1000));
        return caffeineCacheManager;
    }
}
