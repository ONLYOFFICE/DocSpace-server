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

package com.asc.common.autoconfigurations.limiter;

import java.time.temporal.ChronoUnit;
import java.util.List;
import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;

/**
 * Configuration properties for Bucket4j rate limiting. Provides settings for Redis connection and
 * rate limit rules, which can be customized via application properties using the `bucket4j` prefix.
 */
@Data
@ConfigurationProperties(prefix = "bucket4j")
public class Bucket4jConfiguration {

  /** Redis connection properties. */
  private RedisProperties redis;

  /** Rate limit properties. */
  private RateLimitProperties rateLimits;

  /** Configuration properties for Redis connection. */
  @Data
  public static class RedisProperties {
    /** The host of the Redis server. */
    private String host;

    /** The port of the Redis server. */
    private int port;

    /** The Redis database index to use. */
    private int database;

    /** The username for authenticating with the Redis server. */
    private String username;

    /** The password for authenticating with the Redis server. */
    private String password;

    /** Indicates whether SSL is enabled for the Redis connection. */
    private boolean ssl;
  }

  /** Configuration properties for rate limiting. */
  @Data
  public static class RateLimitProperties {
    /** A list of rate limit rules, each defined for a specific client or method. */
    private List<ClientRateLimitProperties> limits;

    /** Configuration properties for individual client rate limiting. */
    @Data
    public static class ClientRateLimitProperties {
      /** The HTTP method to which the rate limit applies (e.g., GET, POST). */
      private String method;

      /** The maximum number of tokens available in the rate-limiting bucket. */
      private int capacity;

      /** Refill properties for replenishing tokens in the rate-limiting bucket. */
      private Refill refill;

      /**
       * Configuration properties for rate limiter refill. Specifies how tokens are replenished over
       * time in the rate-limiting bucket.
       */
      @Data
      public static class Refill {
        /** The number of tokens added to the bucket during each refill period. */
        private int tokens;

        /** The duration of the refill period. */
        private int period;

        /** The time unit for the refill period (e.g., SECONDS, MINUTES). */
        private ChronoUnit timeUnit;
      }
    }
  }
}
