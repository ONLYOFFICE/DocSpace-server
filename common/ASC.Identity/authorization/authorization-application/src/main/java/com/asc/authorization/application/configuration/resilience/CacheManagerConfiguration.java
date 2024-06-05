package com.asc.authorization.application.configuration.resilience;

import com.google.common.cache.CacheBuilder;
import java.util.concurrent.TimeUnit;
import org.springframework.cache.Cache;
import org.springframework.cache.CacheManager;
import org.springframework.cache.concurrent.ConcurrentMapCache;
import org.springframework.cache.concurrent.ConcurrentMapCacheManager;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;

/** Configuration class for managing different cache managers. */
@Configuration
public class CacheManagerConfiguration {

  /**
   * Creates the primary cache manager with a default expiration time of 30 seconds and a maximum
   * size of 1500 entries.
   *
   * @return the primary {@link CacheManager} bean.
   */
  @Bean
  @Primary
  public CacheManager cacheManager() {
    return new ConcurrentMapCacheManager() {
      @Override
      protected Cache createConcurrentMapCache(String name) {
        return new ConcurrentMapCache(
            name,
            CacheBuilder.newBuilder()
                .expireAfterWrite(30, TimeUnit.SECONDS)
                .maximumSize(1500)
                .build()
                .asMap(),
            false);
      }
    };
  }

  /**
   * Creates a cache manager named "clientCacheManager" with a default expiration time of 10 seconds
   * and a maximum size of 500 entries.
   *
   * @return the {@link CacheManager} bean for client cache management.
   */
  @Bean("clientCacheManager")
  public CacheManager clientCacheManager() {
    return new ConcurrentMapCacheManager() {
      @Override
      protected Cache createConcurrentMapCache(String name) {
        return new ConcurrentMapCache(
            name,
            CacheBuilder.newBuilder()
                .expireAfterWrite(10, TimeUnit.SECONDS)
                .maximumSize(500)
                .build()
                .asMap(),
            false);
      }
    };
  }

  /**
   * Creates a cache manager named "ascClientCacheManager" with a default expiration time of 30
   * seconds and a maximum size of 500 entries.
   *
   * @return the {@link CacheManager} bean for ASC client cache management.
   */
  @Bean("ascClientCacheManager")
  public CacheManager ascClientCacheManager() {
    return new ConcurrentMapCacheManager() {
      @Override
      protected Cache createConcurrentMapCache(String name) {
        return new ConcurrentMapCache(
            name,
            CacheBuilder.newBuilder()
                .expireAfterWrite(30, TimeUnit.SECONDS)
                .maximumSize(500)
                .build()
                .asMap(),
            false);
      }
    };
  }
}
