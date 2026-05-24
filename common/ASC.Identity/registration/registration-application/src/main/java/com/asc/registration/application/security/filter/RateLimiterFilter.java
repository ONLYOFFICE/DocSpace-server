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

package com.asc.registration.application.security.filter;

import com.asc.common.utilities.HttpUtils;
import com.asc.registration.application.security.authentication.BasicSignatureTokenPrincipal;
import io.github.bucket4j.BucketConfiguration;
import io.github.bucket4j.ConsumptionProbe;
import io.github.bucket4j.distributed.proxy.ProxyManager;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.concurrent.TimeUnit;
import java.util.function.Function;
import java.util.function.Supplier;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * Filter for enforcing rate limiting on incoming requests based on client IP address.
 *
 * <p>This filter uses the Bucket4j library to manage rate limiting rules and ensure fair usage.
 * Requests exceeding the allowed rate limit are rejected with a 429 Too Many Requests status code,
 * and headers are included in the response to indicate the remaining tokens and reset time.
 */
@Slf4j
@RequiredArgsConstructor
@Component("registrationRateLimiterFilter")
@ConditionalOnProperty(prefix = "bucket4j", name = "enabled", havingValue = "true")
public class RateLimiterFilter extends OncePerRequestFilter {
  /** HTTP response header for remaining rate limit tokens. */
  private static final String X_RATE_REMAINING = "X-Ratelimit-Remaining";

  /** HTTP response header for rate limit reset time. */
  private static final String X_RATE_RESET = "X-Ratelimit-Reset";

  private final Function<HttpMethod, Supplier<BucketConfiguration>> bucketFactory;
  private final ProxyManager<String> proxyManager;
  private final HttpUtils httpUtils;

  /**
   * Filters incoming requests and enforces rate limiting based on client IP.
   *
   * <p>If the client exceeds the allowed rate limit, the response includes headers indicating the
   * remaining tokens and reset time, and returns a 429 Too Many Requests status code.
   *
   * @param request the {@link HttpServletRequest}.
   * @param response the {@link HttpServletResponse}.
   * @param chain the {@link FilterChain}.
   * @throws ServletException if an error occurs during the filter process.
   * @throws IOException if an I/O error occurs during the filter process.
   */
  protected void doFilterInternal(
      HttpServletRequest request, HttpServletResponse response, FilterChain chain)
      throws ServletException, IOException {
    var authentication = SecurityContextHolder.getContext().getAuthentication();
    var method = httpUtils.getHttpMethod(request);
    var clientIdentifier = getClientIp(request);
    var bucketConfiguration = bucketFactory.apply(HttpMethod.valueOf(method));
    if (authentication != null
        && authentication.getPrincipal() instanceof BasicSignatureTokenPrincipal principal)
      clientIdentifier = principal.getUserId();
    if (clientIdentifier != null) {
      var bucket =
          proxyManager
              .builder()
              .build(
                  String.format("identity:registration:%s:%s", method, clientIdentifier),
                  bucketConfiguration);
      var probe = bucket.tryConsumeAndReturnRemaining(1);
      if (probe.isConsumed()) {
        addRateLimitHeaders(response, probe);
        chain.doFilter(request, response);
      } else {
        handleRateLimitExceeded(response, probe);
      }
    } else {
      chain.doFilter(request, response);
    }
  }

  /**
   * Adds rate limit headers to the response.
   *
   * <p>This method sets the headers {@code X-Ratelimit-Remaining} and {@code X-Ratelimit-Reset} to
   * provide feedback about the remaining tokens and the reset time.
   *
   * @param response the {@link HttpServletResponse} to set the headers on.
   * @param probe the {@link ConsumptionProbe} containing rate limit information.
   */
  private void addRateLimitHeaders(HttpServletResponse response, ConsumptionProbe probe) {
    response.setHeader(X_RATE_REMAINING, String.valueOf(probe.getRemainingTokens()));
    response.setHeader(
        X_RATE_RESET,
        String.valueOf(TimeUnit.NANOSECONDS.toSeconds(probe.getNanosToWaitForRefill())));
  }

  /**
   * Handles the response when the rate limit is exceeded.
   *
   * <p>This method returns a 429 Too Many Requests response with headers indicating the remaining
   * tokens and reset time, along with a JSON content type.
   *
   * @param response the {@link HttpServletResponse}.
   * @param probe the {@link ConsumptionProbe} containing rate limit information.
   * @throws IOException if an I/O error occurs during the response handling.
   */
  private void handleRateLimitExceeded(HttpServletResponse response, ConsumptionProbe probe)
      throws IOException {
    response.setContentType("application/json");
    response.setHeader(X_RATE_REMAINING, String.valueOf(probe.getRemainingTokens()));
    response.setHeader(X_RATE_RESET, String.valueOf(probe.getNanosToWaitForRefill()));
    response.setStatus(HttpStatus.TOO_MANY_REQUESTS.value());
  }

  /**
   * Retrieves the client IP address from the request.
   *
   * <p>If the {@code X-Forwarded-For} header is present, the first IP address is used. Otherwise,
   * the remote address from the request is used.
   *
   * @param request the {@link HttpServletRequest}.
   * @return the client IP address.
   */
  private String getClientIp(HttpServletRequest request) {
    var xRealAddress = request.getHeader("X-Real-IP");
    if (xRealAddress != null && !xRealAddress.isBlank()) return xRealAddress.split(",")[0];

    var xForwardedAddress = request.getHeader("X-Forwarded-For");
    if (xForwardedAddress != null && !xForwardedAddress.isBlank())
      return xForwardedAddress.split(",")[0];

    var remoteAddress = request.getRemoteAddr();
    if (remoteAddress != null && !remoteAddress.isBlank()) {
      var trimmedAddress = remoteAddress.trim();
      if (httpUtils.isValidPublicIp(trimmedAddress)) return trimmedAddress;
    }

    return null;
  }
}
