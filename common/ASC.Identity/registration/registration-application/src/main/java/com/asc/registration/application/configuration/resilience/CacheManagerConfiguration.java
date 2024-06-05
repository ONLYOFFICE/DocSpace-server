package com.asc.registration.application.configuration.resilience;

import com.github.benmanes.caffeine.cache.Caffeine;
import java.util.concurrent.TimeUnit;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.cache.caffeine.CaffeineCacheManager;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;

/**
 * The CacheManagerConfiguration class provides configuration for caching using Caffeine cache
 * implementation.
 */
@Slf4j
@Configuration
@EnableCaching
public class CacheManagerConfiguration {
  private final int CACHE_EXPIRATION = 3;
  private final int CACHE_SIZE = 3000;
  private final TimeUnit CACHE_EXPIRATION_UNIT = TimeUnit.MINUTES;

  /**
   * Configures and returns the Caffeine cache configuration bean.
   *
   * @return the Caffeine cache configuration bean
   */
  @Bean
  public Caffeine caffeineConfig() {
    MDC.put("expiration", String.valueOf(CACHE_EXPIRATION));
    MDC.put("expiration_unit", CACHE_EXPIRATION_UNIT.name());
    MDC.put("size", String.valueOf(CACHE_SIZE));
    log.info("Building an in-memory cache");
    MDC.clear();

    return Caffeine.newBuilder()
        .expireAfterWrite(CACHE_EXPIRATION, CACHE_EXPIRATION_UNIT)
        .maximumSize(CACHE_SIZE);
  }

  /**
   * Creates and returns the primary cache manager bean using the provided Caffeine configuration.
   *
   * @param caffeine the Caffeine cache configuration
   * @return the primary cache manager bean
   */
  @Bean
  @Primary
  public CacheManager cacheManager(Caffeine caffeine) {
    var caffeineCacheManager = new CaffeineCacheManager();
    caffeineCacheManager.setCaffeine(caffeine);
    return caffeineCacheManager;
  }

  /**
   * Creates and returns a specific cache manager bean for the 'ascClientCacheManager'.
   *
   * @return the cache manager bean for 'ascClientCacheManager'
   */
  @Bean("ascClientCacheManager")
  public CacheManager ascClientCacheManager() {
    var caffeineCacheManager = new CaffeineCacheManager();
    caffeineCacheManager.setCaffeine(
        Caffeine.newBuilder().expireAfterWrite(30, TimeUnit.SECONDS).maximumSize(1000));
    return caffeineCacheManager;
  }
}
