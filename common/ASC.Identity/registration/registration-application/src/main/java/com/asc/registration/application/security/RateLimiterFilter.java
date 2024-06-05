package com.asc.registration.application.security;

import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.registration.application.configuration.resilience.Bucket4jConfiguration;
import io.github.bucket4j.BucketConfiguration;
import io.github.bucket4j.distributed.proxy.ProxyManager;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.concurrent.TimeUnit;
import java.util.function.Supplier;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

@Slf4j
@Component
@RequiredArgsConstructor
public class RateLimiterFilter extends OncePerRequestFilter {
  private final String X_RATE_LIMIT = "X-Ratelimit-Limit";
  private final String X_RATE_REMAINING = "X-Ratelimit-Remaining";
  private final String X_RATE_RESET = "X-Ratelimit-Reset";

  private final Bucket4jConfiguration bucket4jConfiguration;
  private final Supplier<BucketConfiguration> bucketConfiguration;
  private final ProxyManager<String> proxyManager;

  /**
   * Filters requests to enforce rate limiting based on client id.
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
    if (request.getAttribute("person") instanceof AscPersonResponse person) {
      var bucket = proxyManager.builder().build(person.getId(), bucketConfiguration);
      var probe = bucket.tryConsumeAndReturnRemaining(1);
      if (probe.isConsumed()) {
        chain.doFilter(request, response);
      } else {
        response.setContentType("application/json");
        response.setHeader(
            X_RATE_LIMIT,
            String.valueOf(
                bucket4jConfiguration.getRateLimits().getClientRateLimit().getCapacity()));
        response.setHeader(X_RATE_REMAINING, String.valueOf(probe.getRemainingTokens()));
        response.setHeader(
            X_RATE_RESET,
            String.valueOf(TimeUnit.NANOSECONDS.toSeconds(probe.getNanosToWaitForRefill())));
        response.setStatus(429);
      }
    } else {
      chain.doFilter(request, response);
    }
  }
}
