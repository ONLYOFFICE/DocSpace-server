package com.asc.registration.application.exception.handler;

import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/** The DatabaseExceptionHandler class handles exceptions related to database operations. */
@Slf4j
@ControllerAdvice
public class DatabaseExceptionHandler {
  /**
   * Handles the DataAccessException and returns a ResponseEntity with an internal server error
   * status.
   *
   * @param ex the DataAccessException that was raised
   * @param request the HttpServletRequest associated with the exception
   * @return a ResponseEntity with an internal server error status
   */
  @ExceptionHandler(DataAccessException.class)
  public ResponseEntity<?> handleDataAccessException(
      DataAccessException ex, HttpServletRequest request) {
    log.error("Could not perform a database operation", ex);
    return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
  }
}
