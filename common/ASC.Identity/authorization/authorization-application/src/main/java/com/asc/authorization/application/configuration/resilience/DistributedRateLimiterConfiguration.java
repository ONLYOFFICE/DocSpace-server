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
import java.util.function.Supplier;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

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
  public Supplier<BucketConfiguration> bucketConfiguration() {
    Bucket4jConfiguration.RateLimitProperties.ClientRateLimitProperties rateLimitProperties =
        bucket4jConfiguration.getRateLimits().getClientRateLimit();
    return () ->
        BucketConfiguration.builder()
            .addLimit(
                Bandwidth.classic(
                    rateLimitProperties.getCapacity(),
                    Refill.greedy(
                        rateLimitProperties.getRefill().getTokens(),
                        Duration.of(
                            rateLimitProperties.getRefill().getPeriod(),
                            rateLimitProperties.getRefill().getTimeUnit()))))
            .build();
  }
}
