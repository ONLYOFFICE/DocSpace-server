package com.asc.authorization.application.exception.handler;

import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/**
 * Global exception handler for handling any unhandled exceptions.
 *
 * <p>This class handles all types of {@link Exception} thrown by any controller and returns a
 * standard HTTP response with an appropriate status code.
 */
@Slf4j
@ControllerAdvice
public class UnhandledExceptionHandler {

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
