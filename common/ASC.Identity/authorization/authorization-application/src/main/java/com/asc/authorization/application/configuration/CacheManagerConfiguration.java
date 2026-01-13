// (c) Copyright Ascensio System SIA 2009-2025
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
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.authorization.application.configuration;

import com.asc.authorization.application.configuration.serialization.CacheObjectSerializer;
import com.fasterxml.jackson.annotation.JsonTypeInfo;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.databind.jsontype.impl.LaissezFaireSubTypeValidator;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import java.time.Duration;
import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.data.redis.cache.RedisCacheConfiguration;
import org.springframework.data.redis.cache.RedisCacheManager;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import org.springframework.data.redis.serializer.RedisSerializationContext;
import org.springframework.data.redis.serializer.RedisSerializer;

/**
 * Configuration class for setting up cache management using Redis cache.
 *
 * <p>This configuration provides a {@link RedisCacheManager} bean with pre-configured cache
 * settings for application-wide caching needs.
 *
 * @see RedisCacheManager
 * @see RedisCacheConfiguration
 */
@Configuration
@EnableCaching
public class CacheManagerConfiguration {
  /**
   * Creates a custom ObjectMapper for Redis serialization with type information.
   *
   * @return a configured {@link ObjectMapper} instance
   */
  @Bean
  public ObjectMapper redisObjectMapper() {
    var mapper = new ObjectMapper();
    mapper.registerModule(new JavaTimeModule());
    mapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);
    mapper.activateDefaultTyping(
        LaissezFaireSubTypeValidator.instance,
        ObjectMapper.DefaultTyping.NON_FINAL,
        JsonTypeInfo.As.PROPERTY);

    return mapper;
  }

  /**
   * Creates a custom Redis serializer using the configured ObjectMapper.
   *
   * @param objectMapper the custom ObjectMapper for JSON serialization
   * @return a configured {@link CacheObjectSerializer} instance
   */
  @Bean
  public CacheObjectSerializer cacheObjectSerializer(ObjectMapper objectMapper) {
    return new CacheObjectSerializer(objectMapper);
  }

  /**
   * Builds and configures a Redis cache configuration with specific eviction policies.
   *
   * @param cacheSerializer the custom serializer for cache values
   * @return a configured {@link RedisCacheConfiguration} instance
   */
  private RedisCacheConfiguration redisCacheConfiguration(CacheObjectSerializer cacheSerializer) {
    return RedisCacheConfiguration.defaultCacheConfig()
        .entryTtl(Duration.ofMinutes(10))
        .disableKeyPrefix()
        .serializeKeysWith(
            RedisSerializationContext.SerializationPair.fromSerializer(RedisSerializer.string()))
        .serializeValuesWith(
            RedisSerializationContext.SerializationPair.fromSerializer(cacheSerializer));
  }

  /**
   * Creates and configures a {@link RedisCacheManager} bean for Spring's cache abstraction.
   *
   * <p>The cache manager is initialized with Redis connection and applies the configuration from
   * {@link #redisCacheConfiguration(CacheObjectSerializer)}.
   *
   * @param connectionFactory the Redis connection factory
   * @param cacheSerializer the custom serializer for cache values
   * @return a configured {@link RedisCacheManager} instance
   * @see #redisCacheConfiguration(CacheObjectSerializer)
   */
  @Bean
  public CacheManager cacheManager(
      RedisConnectionFactory connectionFactory, CacheObjectSerializer cacheSerializer) {
    return RedisCacheManager.builder(connectionFactory)
        .cacheDefaults(redisCacheConfiguration(cacheSerializer))
        .transactionAware()
        .build();
  }
}
