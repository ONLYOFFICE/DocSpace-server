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

package com.asc.registration.application.security.filter;

import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.utilities.HttpUtils;
import io.github.bucket4j.Bucket;
import io.github.bucket4j.BucketConfiguration;
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

@Slf4j
@Component
@RequiredArgsConstructor
public class RateLimiterFilter extends OncePerRequestFilter {
  private final String X_RATE_REMAINING = "X-Ratelimit-Remaining";
  private final String X_RATE_RESET = "X-Ratelimit-Reset";

  private final HttpUtils httpUtils;
  private final Function<HttpMethod, Supplier<BucketConfiguration>> bucketFactory;
  private final ProxyManager<String> proxyManager;

  /**
   * Filters requests to enforce rate limiting based on client id or IP address.
   *
   * @param request the HttpServletRequest.
   * @param response the HttpServletResponse.
   * @param chain the FilterChain.
   * @throws ServletException if an error occurs during the filter process.
   * @throws IOException if an I/O error occurs during the filter process.
   */
  protected void doFilterInternal(
      HttpServletRequest request, HttpServletResponse response, FilterChain chain)
      throws ServletException, IOException {
    var method = request.getMethod();
    var bucketConfiguration = bucketFactory.apply(HttpMethod.valueOf(method));
    if (request.getAttribute("person") instanceof AscPersonResponse person) {
      var bucket =
          proxyManager
              .builder()
              .build(String.format("registration:%s:%s", method, person.getId()), bucketConfiguration);
      handleRequest(bucket, request, response, chain);
    } else {
      var clientIp = httpUtils.getRequestClientAddress(request).orElse(request.getRemoteAddr());
      if (clientIp.isEmpty()) {
        response.setStatus(HttpStatus.FORBIDDEN.value());
        return;
      }

      var bucket =
          proxyManager
              .builder()
              .build(String.format("%s:%s", method, clientIp), bucketConfiguration);
      handleRequest(bucket, request, response, chain);
    }
  }

  private void handleRequest(
      Bucket bucket, HttpServletRequest request, HttpServletResponse response, FilterChain chain)
      throws IOException, ServletException {
    var probe = bucket.tryConsumeAndReturnRemaining(1);
    response.setHeader(X_RATE_REMAINING, String.valueOf(probe.getRemainingTokens()));
    response.setHeader(
        X_RATE_RESET,
        String.valueOf(probe.getNanosToWaitForReset()));
    response.setStatus(HttpStatus.TOO_MANY_REQUESTS.value());
    if (probe.isConsumed()) {
      chain.doFilter(request, response);
    }
  }
}
