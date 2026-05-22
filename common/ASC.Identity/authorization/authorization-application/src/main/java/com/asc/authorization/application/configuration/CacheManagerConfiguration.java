// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
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

package com.asc.authorization.application.configuration;

import com.asc.authorization.application.configuration.serialization.CacheObjectSerializer;
import com.fasterxml.jackson.annotation.JsonTypeInfo;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.databind.jsontype.impl.LaissezFaireSubTypeValidator;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import java.time.Duration;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
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
 * <p>This configuration is only loaded when Redis classes are available on the classpath.
 *
 * @see RedisCacheManager
 * @see RedisCacheConfiguration
 */
@Configuration
@EnableCaching
@ConditionalOnClass(RedisConnectionFactory.class)
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
