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
