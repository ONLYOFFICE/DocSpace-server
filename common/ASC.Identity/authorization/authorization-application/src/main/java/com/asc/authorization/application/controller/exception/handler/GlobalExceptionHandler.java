package com.asc.authorization.application.controller.exception.handler;

import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.ErrorResponse;
import org.springframework.web.bind.MissingServletRequestParameterException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.servlet.resource.NoResourceFoundException;

/**
 * GlobalExceptionHandler is a centralized exception handler that intercepts and processes various
 * exceptions thrown during application execution. Each handler method returns a {@link
 * ResponseEntity} with an appropriate HTTP status code.
 */
@Slf4j
@ControllerAdvice
public class GlobalExceptionHandler {
  /**
   * Handles {@link DataAccessException} and returns a response with HTTP status 500.
   *
   * <p>This method logs the exception and returns a {@link ResponseEntity} with HTTP status {@link
   * HttpStatus#INTERNAL_SERVER_ERROR}.
   *
   * @param ex the {@link DataAccessException} thrown during a database operation
   * @param request the {@link HttpServletRequest} in which the exception was raised
   * @return a {@link ResponseEntity} with HTTP status 500 (Internal Server Error)
   */
  @ExceptionHandler(DataAccessException.class)
  public ResponseEntity<?> handleDataAccessException(
      DataAccessException ex, HttpServletRequest request) {
    log.error("Could not perform a database operation", ex);
    return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
  }

  /**
   * Handles {@link MissingServletRequestParameterException} and returns a response with HTTP status
   * 400.
   *
   * <p>This method logs a warning and returns a {@link ResponseEntity} with HTTP status {@link
   * HttpStatus#BAD_REQUEST}.
   *
   * @param ex the {@link MissingServletRequestParameterException} thrown when a required request
   *     parameter is missing
   * @param request the {@link HttpServletRequest} in which the exception was raised
   * @return a {@link ResponseEntity} with HTTP status 400 (Bad Request)
   */
  @ExceptionHandler(MissingServletRequestParameterException.class)
  public ResponseEntity<?> handleMissingParameterException(
      MissingServletRequestParameterException ex, HttpServletRequest request) {
    log.warn("Request parameter is missing", ex);
    return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
  }

  /**
   * Handles the {@link NoResourceFoundException} and returns an error response with HTTP 404
   * status.
   *
   * <p>This method ensures that clients receive a structured error message when requesting a
   * non-existent endpoint.
   *
   * @param ex the {@code NoResourceFoundException} that was raised when the requested resource was
   *     not found.
   * @return an {@code ErrorResponse} containing the error details and HTTP status code {@code
   *     NOT_FOUND}.
   */
  @ExceptionHandler(value = {NoResourceFoundException.class})
  @ResponseStatus(HttpStatus.NOT_FOUND)
  public ErrorResponse handleNoResourceFoundException(NoResourceFoundException ex) {
    return ErrorResponse.create(ex, HttpStatus.NOT_FOUND, ex.getMessage());
  }

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

  /**
   * Handles any {@link Exception} and returns a response with HTTP status 500.
   *
   * <p>This method logs the exception and returns a {@link ResponseEntity} with HTTP status {@link
   * HttpStatus#INTERNAL_SERVER_ERROR}.
   *
   * @param ex the {@link Exception} thrown during an operation
   * @param request the {@link HttpServletRequest} in which the exception was raised
   * @return a {@link ResponseEntity} with HTTP status 500 (Internal Server Error)
   */
  @ExceptionHandler(Exception.class)
  public final ResponseEntity<?> handleAllExceptions(Exception ex, HttpServletRequest request) {
    log.error("Could not perform an action. Unknown exception", ex);
    return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
  }
}
