package com.asc.registration.application.configuration.resilience;

import java.time.temporal.ChronoUnit;
import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/** Configuration properties for Bucket4j rate limiting. */
@Data
@Configuration
@ConfigurationProperties(prefix = "bucket4j")
public class Bucket4jConfiguration {

  /** Redis connection properties. */
  private RedisProperties redis;

  /** Rate limit properties. */
  private RateLimitProperties rateLimits;

  /** Configuration properties for Redis connection. */
  @Data
  public static class RedisProperties {
    /** The Redis server host. */
    private String host;

    /** The Redis server port. */
    private int port;

    /** The Redis server username. */
    private String username;

    /** The Redis server password. */
    private String password;

    /** Whether SSL is enabled for Redis connection. */
    private boolean ssl;
  }

  /** Configuration properties for rate limiting. */
  @Data
  public static class RateLimitProperties {
    /** Properties for client rate limiting. */
    private ClientRateLimitProperties clientRateLimit;

    /** Configuration properties for client rate limiting. */
    @Data
    public static class ClientRateLimitProperties {
      /** The maximum number of tokens available in the bucket. */
      private int capacity;

      /** Refill properties for the rate limiter. */
      private Refill refill;

      /** Configuration properties for rate limiter refill. */
      @Data
      public static class Refill {
        /** The number of tokens to add to the bucket each period. */
        private int tokens;

        /** The period over which the tokens are added. */
        private int period;

        /** The unit of time for the refill period. */
        private ChronoUnit timeUnit;
      }
    }
  }
}
