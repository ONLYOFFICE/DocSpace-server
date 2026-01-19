// (c) Copyright Ascensio System SIA 2009-2026
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
    var factory = new LettuceConnectionFactory(config);
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
