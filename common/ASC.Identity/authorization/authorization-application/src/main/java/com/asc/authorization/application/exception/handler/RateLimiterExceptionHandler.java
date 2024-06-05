package com.asc.authorization.application.exception.handler;

import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/** Global exception handler for handling rate limiter exceptions. */
@Slf4j
@ControllerAdvice
public class RateLimiterExceptionHandler {

  /**
   * Handles {@link RequestNotPermitted} exceptions, which are thrown when a request is not
   * permitted by the rate limiter.
   *
   * @param ex the exception that was thrown.
   * @param request the {@link HttpServletRequest} that resulted in the exception.
   * @return a {@link ResponseEntity} with status code 429 (Too Many Requests).
   */
  @ExceptionHandler(value = {RequestNotPermitted.class})
  public ResponseEntity<?> handleRequestNotPermitted(Throwable ex, HttpServletRequest request) {
    log.warn("Request not permitted by a rate-limiter", ex);
    return ResponseEntity.status(HttpStatus.TOO_MANY_REQUESTS).build();
  }
}
