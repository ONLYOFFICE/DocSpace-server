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

// TODO: Register as autoconfiguration later
package com.asc.common.autoconfigurations.limiter;

import io.github.bucket4j.Bandwidth;
import io.github.bucket4j.BucketConfiguration;
import io.github.bucket4j.Refill;
import io.github.bucket4j.distributed.ExpirationAfterWriteStrategy;
import io.github.bucket4j.redis.lettuce.cas.LettuceBasedProxyManager;
import io.lettuce.core.RedisClient;
import io.lettuce.core.RedisURI;
import io.lettuce.core.codec.ByteArrayCodec;
import io.lettuce.core.codec.RedisCodec;
import io.lettuce.core.codec.StringCodec;
import java.time.Duration;
import java.util.List;
import java.util.function.Function;
import java.util.function.Supplier;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;

/**
 * Configuration class for setting up distributed rate limiting using Bucket4j and Redis. This
 * configuration is activated when the property `bucket4j.enabled` is set to `true`. It provides
 * beans for managing the Redis connection, Bucket4j proxy manager, and bucket configurations.
 */
@Configuration
@RequiredArgsConstructor
@EnableConfigurationProperties(Bucket4jConfiguration.class)
@ConditionalOnProperty(prefix = "bucket4j", name = "enabled", havingValue = "true")
public class DistributedRateLimiterAutoconfigurationConfiguration {
  private final Bucket4jConfiguration bucket4jConfiguration;

  /**
   * Creates and configures a Redis client based on the application properties. The Redis client is
   * used for establishing connections to the Redis server for distributed rate limiting.
   *
   * @return The configured {@link RedisClient}.
   */
  @Bean
  public RedisClient redisClient() {
    return RedisClient.create(
        RedisURI.builder()
            .withHost(bucket4jConfiguration.getRedis().getHost())
            .withPort(bucket4jConfiguration.getRedis().getPort())
            .withDatabase(bucket4jConfiguration.getRedis().getDatabase())
            .withSsl(bucket4jConfiguration.getRedis().isSsl())
            .withAuthentication(
                bucket4jConfiguration.getRedis().getUsername(),
                bucket4jConfiguration.getRedis().getPassword())
            .build());
  }

  /**
   * Creates and configures a Lettuce-based proxy manager for managing distributed rate limits. The
   * proxy manager uses Redis to persist rate-limiting data and supports expiration strategies.
   *
   * @param redisClient The Redis client used to establish a connection to the Redis server.
   * @return The configured {@link LettuceBasedProxyManager} instance for distributed rate limiting.
   */
  @Bean
  public LettuceBasedProxyManager<String> lettuceBasedProxyManager(RedisClient redisClient) {
    var redisConnection =
        redisClient.connect(RedisCodec.of(StringCodec.UTF8, ByteArrayCodec.INSTANCE));
    return LettuceBasedProxyManager.builderFor(redisConnection)
        .withExpirationStrategy(
            ExpirationAfterWriteStrategy.basedOnTimeForRefillingBucketUpToMax(
                Duration.ofMinutes(1L)))
        .build();
  }

  /**
   * Creates a supplier for Bucket4j configurations based on the application-defined rate limits.
   * This supplier provides Bucket configurations for each HTTP method based on the defined rate
   * limit properties.
   *
   * @return A {@link Function} mapping {@link HttpMethod} to a {@link Supplier} of {@link
   *     BucketConfiguration}.
   * @throws Exception If no configuration for the GET method is found, an exception is thrown.
   */
  @Bean
  public Function<HttpMethod, Supplier<BucketConfiguration>> bucketConfiguration()
      throws Exception {
    List<Bucket4jConfiguration.RateLimitProperties.ClientRateLimitProperties> rateLimitProperties =
        bucket4jConfiguration.getRateLimits().getLimits();
    Bucket4jConfiguration.RateLimitProperties.ClientRateLimitProperties getLimits =
        bucket4jConfiguration.getRateLimits().getLimits().stream()
            .filter(props -> props.getMethod().equalsIgnoreCase(HttpMethod.GET.name()))
            .findFirst()
            .orElseThrow(() -> new Exception("Could not initialize rate-limiter configuration"));
    return (HttpMethod method) -> {
      var config =
          rateLimitProperties.stream()
              .filter(props -> props.getMethod().equalsIgnoreCase(method.name()))
              .findFirst()
              .orElse(getLimits);
      return () ->
          BucketConfiguration.builder()
              .addLimit(
                  Bandwidth.classic(
                      config.getCapacity(),
                      Refill.greedy(
                          config.getRefill().getTokens(),
                          Duration.of(
                              config.getRefill().getPeriod(), config.getRefill().getTimeUnit()))))
              .build();
    };
  }
}
