package com.onlyoffice.authorization.web.security.filters;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.redisson.api.RedissonClient;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;


@Slf4j
@Component
@RequiredArgsConstructor
public class DistributedRateLimiterFilter extends OncePerRequestFilter {
    private final String RATE_LIMITER_NAME = "identityAuthorizationRateLimiter";
    private final String X_RATE_LIMIT = "X-Ratelimit-Limit";
    private final String X_RATE_REMAINING = "X-Ratelimit-Remaining";
    private final String X_RATE_RESET = "X-Ratelimit-Reset";

    private final RedissonClient redissonClient;

    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain) throws ServletException, IOException {
        var limiter = redissonClient.getRateLimiter(RATE_LIMITER_NAME);
        if (limiter == null) {
            log.error("Could not get an instance of distributed rate-limiter");
            response.setStatus(HttpServletResponse.SC_INTERNAL_SERVER_ERROR);
            return;
        }

        response.setHeader(X_RATE_LIMIT, String.valueOf(limiter.getConfig().getRate()));
        response.setHeader(X_RATE_REMAINING, String.valueOf(limiter.availablePermits()));
        response.setHeader(X_RATE_RESET, String.valueOf(limiter.getConfig().getRateInterval()));
        if (!limiter.tryAcquire(1)) {
            log.debug("Could not acquire a rate-limiter permission");
            response.setStatus(HttpStatus.TOO_MANY_REQUESTS.value());
            return;
        }

        response.setHeader(X_RATE_REMAINING, String.valueOf(limiter.availablePermits()));
        filterChain.doFilter(request, response);
    }
}
