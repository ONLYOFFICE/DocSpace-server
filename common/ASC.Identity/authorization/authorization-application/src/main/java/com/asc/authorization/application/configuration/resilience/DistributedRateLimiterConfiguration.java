// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.authorization.application.configuration.resilience;

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
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;

/** Configuration class for setting up the distributed rate limiter using Bucket4j and Redis. */
@Configuration
@RequiredArgsConstructor
public class DistributedRateLimiterConfiguration {
  private final Bucket4jConfiguration bucket4jConfiguration;

  /**
   * Creates and configures a Redis client based on the application configuration properties.
   *
   * @return the configured {@link RedisClient}.
   */
  @Bean
  public RedisClient redisClient() {
    return RedisClient.create(
        RedisURI.builder()
            .withHost(bucket4jConfiguration.getRedis().getHost())
            .withPort(bucket4jConfiguration.getRedis().getPort())
            .withSsl(bucket4jConfiguration.getRedis().isSsl())
            .withAuthentication(
                bucket4jConfiguration.getRedis().getUsername(),
                bucket4jConfiguration.getRedis().getPassword())
            .build());
  }

  /**
   * Creates and configures a Lettuce-based proxy manager for distributed rate limiting.
   *
   * @param redisClient the Redis client to use for connecting to Redis.
   * @return the configured {@link LettuceBasedProxyManager} instance.
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
   * Creates a supplier for bucket configurations based on the application configuration properties.
   *
   * @return the supplier of {@link BucketConfiguration}.
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
