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

package com.asc.identity.minified.config;

import com.github.benmanes.caffeine.cache.Caffeine;
import io.github.bucket4j.Bandwidth;
import io.github.bucket4j.BucketConfiguration;
import io.github.bucket4j.Refill;
import io.github.bucket4j.caffeine.CaffeineProxyManager;
import io.github.bucket4j.distributed.proxy.ProxyManager;
import java.time.Duration;
import java.util.function.Function;
import java.util.function.Supplier;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;
import org.springframework.http.HttpMethod;

/**
 * In-memory rate limiter configuration for minified deployment.
 *
 * <p>Provides rate limiting using local in-memory buckets with Caffeine instead of Redis-based
 * distributed rate limiting. This is suitable for single-instance deployments.
 */
@Slf4j
@Configuration
@Profile("minified")
public class InMemoryRateLimiterConfiguration {
  private static final long DEFAULT_CAPACITY = 100;
  private static final long DEFAULT_REFILL_TOKENS = 100;
  private static final Duration DEFAULT_REFILL_PERIOD = Duration.ofMinutes(1);

  @Bean
  @ConditionalOnMissingBean
  public Function<HttpMethod, Supplier<BucketConfiguration>> bucketConfiguration() {
    log.info("Initializing in-memory rate limiter bucket configuration");
    return (HttpMethod method) ->
        () ->
            BucketConfiguration.builder()
                .addLimit(
                    Bandwidth.classic(
                        DEFAULT_CAPACITY,
                        Refill.greedy(DEFAULT_REFILL_TOKENS, DEFAULT_REFILL_PERIOD)))
                .build();
  }

  @Bean
  @ConditionalOnMissingBean
  @SuppressWarnings("unchecked")
  public ProxyManager<String> inMemoryProxyManager() {
    log.info("Initializing Caffeine-based in-memory rate limiter proxy manager");
    Caffeine<String, io.github.bucket4j.distributed.remote.RemoteBucketState> builder =
        (Caffeine<String, io.github.bucket4j.distributed.remote.RemoteBucketState>)
            (Caffeine<?, ?>) Caffeine.newBuilder().maximumSize(100_000);
    return new CaffeineProxyManager<>(builder, Duration.ofMinutes(1));
  }
}
