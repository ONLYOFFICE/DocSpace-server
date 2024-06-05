package com.asc.authorization.application.exception.handler;

import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/**
 * Global exception handler for handling database-related exceptions.
 *
 * <p>This class handles {@link DataAccessException} thrown by any controller and returns a standard
 * HTTP response with an appropriate status code.
 */
@Slf4j
@ControllerAdvice
public class DatabaseExceptionHandler {

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
}
