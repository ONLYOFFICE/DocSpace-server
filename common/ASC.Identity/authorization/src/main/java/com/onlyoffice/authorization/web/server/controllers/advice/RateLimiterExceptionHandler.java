/**
 *
 */
package com.onlyoffice.authorization.web.server.controllers.advice;

import com.onlyoffice.authorization.core.exceptions.DistributedRateLimiterException;
import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/**
 *
 */
@Slf4j
@ControllerAdvice
public class RateLimiterExceptionHandler {
    @ExceptionHandler(value = {RequestNotPermitted.class, DistributedRateLimiterException.class})
    public ResponseEntity handleRequestNotPermitted(Throwable ex, HttpServletRequest request) {
        log.error(ex.getMessage());
        return ResponseEntity.status(HttpStatus.TOO_MANY_REQUESTS).build();
    }
}
