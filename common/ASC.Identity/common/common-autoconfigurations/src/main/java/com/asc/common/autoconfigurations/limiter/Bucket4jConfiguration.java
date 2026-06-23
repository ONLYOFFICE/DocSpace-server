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
