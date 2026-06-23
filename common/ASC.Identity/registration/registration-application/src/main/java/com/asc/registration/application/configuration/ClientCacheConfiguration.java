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

package com.asc.registration.application.configuration;

import com.asc.registration.application.configuration.serialization.ClientDeserializer;
import com.asc.registration.application.configuration.serialization.ClientSerializer;
import com.asc.registration.core.domain.entity.Client;
import com.fasterxml.jackson.annotation.JsonAutoDetect;
import com.fasterxml.jackson.annotation.PropertyAccessor;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.MapperFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.databind.json.JsonMapper;
import com.fasterxml.jackson.databind.module.SimpleModule;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import org.springframework.data.redis.connection.RedisStandaloneConfiguration;
import org.springframework.data.redis.connection.lettuce.LettuceClientConfiguration;
import org.springframework.data.redis.connection.lettuce.LettuceConnectionFactory;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.data.redis.serializer.RedisSerializer;
import org.springframework.data.redis.serializer.StringRedisSerializer;

/**
 * Configuration class for setting up Redis cache for client caching.
 *
 * <p>This configuration provides RedisTemplate bean for the two-level cache implementation. It uses
 * independent Redis connection properties that can be configured separately from other Redis
 * configurations.
 *
 * <p>This configuration is enabled when `client.cache.redis.enabled=true`.
 */
@Slf4j
@Configuration
@EnableConfigurationProperties(ClientCacheConfigurationProperties.class)
@ConditionalOnProperty(prefix = "client.cache.redis", name = "enabled", havingValue = "true")
public class ClientCacheConfiguration {
  private final ClientCacheConfigurationProperties properties;

  public ClientCacheConfiguration(ClientCacheConfigurationProperties properties) {
    this.properties = properties;
    log.info(
        "ClientCacheConfiguration initialized with Redis: {}:{}/{}",
        properties.getHost(),
        properties.getPort(),
        properties.getDatabase());
  }

  /**
   * Creates a Redis connection factory for the client cache.
   *
   * @return The configured {@link LettuceConnectionFactory}.
   */
  @Bean
  public RedisConnectionFactory clientCacheRedisConnectionFactory() {
    log.info(
        "Creating clientCacheRedisConnectionFactory for {}:{}/{}",
        properties.getHost(),
        properties.getPort(),
        properties.getDatabase());

    var config = new RedisStandaloneConfiguration();
    config.setHostName(properties.getHost());
    config.setPort(properties.getPort());
    config.setDatabase(properties.getDatabase());
    if (properties.getUsername() != null && !properties.getUsername().isEmpty())
      config.setUsername(properties.getUsername());
    if (properties.getPassword() != null && !properties.getPassword().isEmpty())
      config.setPassword(properties.getPassword());

    var clientConfigBuilder = LettuceClientConfiguration.builder();
    if (properties.isSsl()) clientConfigBuilder.useSsl();

    var factory = new LettuceConnectionFactory(config, clientConfigBuilder.build());
    factory.afterPropertiesSet();

    log.info("clientCacheRedisConnectionFactory created successfully");
    return factory;
  }

  /**
   * Creates a RedisTemplate for client cache operations with JSON serialization.
   *
   * <p>Uses {@link RedisSerializer#json()} with a custom {@link ObjectMapper} that includes a
   * custom deserializer for the {@link Client} entity. The deserializer uses the Builder pattern to
   * properly reconstruct Client objects without requiring changes to the domain model.
   *
   * @param clientCacheRedisConnectionFactory The Redis connection factory for client cache.
   * @return The configured {@link RedisTemplate} for String keys and Client values.
   */
  @Bean
  public RedisTemplate<String, Object> clientCacheRedisTemplate(
      @Qualifier("clientCacheRedisConnectionFactory")
          RedisConnectionFactory clientCacheRedisConnectionFactory) {
    log.info("Creating clientCacheRedisTemplate bean");

    var clientModule = new SimpleModule();
    clientModule.addDeserializer(Client.class, new ClientDeserializer());

    var objectMapper =
        JsonMapper.builder()
            .findAndAddModules()
            .addModule(clientModule)
            .configure(SerializationFeature.FAIL_ON_EMPTY_BEANS, false)
            .configure(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS, false)
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
            .configure(MapperFeature.AUTO_DETECT_GETTERS, false)
            .configure(MapperFeature.AUTO_DETECT_SETTERS, false)
            .visibility(PropertyAccessor.FIELD, JsonAutoDetect.Visibility.ANY)
            .build();

    var template = new RedisTemplate<String, Object>();
    template.setConnectionFactory(clientCacheRedisConnectionFactory);
    template.setKeySerializer(new StringRedisSerializer());

    var valueSerializer = new ClientSerializer(objectMapper);

    template.setValueSerializer(valueSerializer);
    template.setHashKeySerializer(new StringRedisSerializer());
    template.setHashValueSerializer(valueSerializer);
    template.afterPropertiesSet();

    log.info("clientCacheRedisTemplate created successfully");
    return template;
  }
}
