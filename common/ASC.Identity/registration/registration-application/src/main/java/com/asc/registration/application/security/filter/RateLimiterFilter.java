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

package com.asc.registration.application.security.filter;

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
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
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
@Component
@RequiredArgsConstructor
public class RateLimiterFilter extends OncePerRequestFilter {
  /** HTTP response header for remaining rate limit tokens. */
  private static final String X_RATE_REMAINING = "X-Ratelimit-Remaining";

  /** HTTP response header for rate limit reset time. */
  private static final String X_RATE_RESET = "X-Ratelimit-Reset";

  private final Function<HttpMethod, Supplier<BucketConfiguration>> bucketFactory;
  private final ProxyManager<String> proxyManager;

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
    var method = request.getMethod();
    var bucketConfiguration = bucketFactory.apply(HttpMethod.valueOf(method));
    var clientIp = getClientIp(request);
    if (clientIp != null) {
      var bucket =
          proxyManager
              .builder()
              .build(String.format("authorization:%s:%s", method, clientIp), bucketConfiguration);
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
    response.setHeader(X_RATE_RESET, String.valueOf(probe.getNanosToWaitForReset()));
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
    var ipAddress = request.getHeader("X-Forwarded-For");
    if (ipAddress == null || ipAddress.isBlank()) {
      ipAddress = request.getRemoteAddr();
    } else {
      ipAddress = ipAddress.split(",")[0];
    }
    return ipAddress;
  }
}
