package com.asc.authorization.application.security.filters;

import com.asc.authorization.application.configuration.resilience.Bucket4jConfiguration;
import io.github.bucket4j.BucketConfiguration;
import io.github.bucket4j.ConsumptionProbe;
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
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/** Filter for rate limiting requests based on client IP using Bucket4j. */
@Slf4j
@Component
@RequiredArgsConstructor
public class RateLimiterFilter extends OncePerRequestFilter {
  private static final String X_RATE_LIMIT = "X-Ratelimit-Limit";
  private static final String X_RATE_REMAINING = "X-Ratelimit-Remaining";
  private static final String X_RATE_RESET = "X-Ratelimit-Reset";

  private final Bucket4jConfiguration bucket4jConfiguration;
  private final Supplier<BucketConfiguration> bucketConfiguration;
  private final ProxyManager<String> proxyManager;

  /**
   * Filters requests to enforce rate limiting based on client IP.
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
    var clientIp = getClientIp(request);
    if (clientIp != null) {
      var bucket = proxyManager.builder().build(clientIp, bucketConfiguration);
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
   * @param response the HttpServletResponse to set the headers on.
   * @param probe the ConsumptionProbe containing rate limit information.
   */
  private void addRateLimitHeaders(HttpServletResponse response, ConsumptionProbe probe) {
    response.setHeader(
        X_RATE_LIMIT,
        String.valueOf(bucket4jConfiguration.getRateLimits().getClientRateLimit().getCapacity()));
    response.setHeader(X_RATE_REMAINING, String.valueOf(probe.getRemainingTokens()));
    response.setHeader(
        X_RATE_RESET,
        String.valueOf(TimeUnit.NANOSECONDS.toSeconds(probe.getNanosToWaitForRefill())));
  }

  /**
   * Handles the response when the rate limit is exceeded.
   *
   * @param response the HttpServletResponse.
   * @param probe the ConsumptionProbe containing rate limit information.
   * @throws IOException if an I/O error occurs during the response handling.
   */
  private void handleRateLimitExceeded(HttpServletResponse response, ConsumptionProbe probe)
      throws IOException {
    response.setContentType("application/json");
    response.setHeader(
        X_RATE_LIMIT,
        String.valueOf(bucket4jConfiguration.getRateLimits().getClientRateLimit().getCapacity()));
    response.setHeader(X_RATE_REMAINING, String.valueOf(probe.getRemainingTokens()));
    response.setHeader(
        X_RATE_RESET,
        String.valueOf(TimeUnit.NANOSECONDS.toSeconds(probe.getNanosToWaitForRefill())));
    response.setStatus(HttpStatus.TOO_MANY_REQUESTS.value());
  }

  /**
   * Retrieves the client IP from the request.
   *
   * @param request the HttpServletRequest.
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
