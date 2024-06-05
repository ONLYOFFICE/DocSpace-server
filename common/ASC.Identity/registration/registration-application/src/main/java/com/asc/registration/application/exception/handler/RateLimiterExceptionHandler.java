package com.asc.registration.application.exception.handler;

import com.asc.registration.application.transfer.ErrorResponse;
import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/** The RateLimiterExceptionHandler class handles exceptions related to rate limiting requests. */
@Slf4j
@ControllerAdvice
public class RateLimiterExceptionHandler {
  /**
   * Handles the RequestNotPermitted exception and returns a ResponseEntity with a status of
   * TOO_MANY_REQUESTS.
   *
   * @param ex the RequestNotPermitted exception that was raised
   * @param request the HttpServletRequest associated with the exception
   * @return a ResponseEntity containing an ErrorResponse and HTTP status code TOO_MANY_REQUESTS
   */
  @ExceptionHandler(value = RequestNotPermitted.class)
  public ResponseEntity<ErrorResponse> handleRequestNotPermitted(
      RequestNotPermitted ex, HttpServletRequest request) {
    log.warn("Rate limiter has blocked current call", ex);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason("Too many requests").build(), HttpStatus.TOO_MANY_REQUESTS);
  }
}
