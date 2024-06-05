package com.asc.registration.application.exception.handler;

import com.asc.registration.application.transfer.ErrorResponse;
import jakarta.servlet.http.HttpServletRequest;
import java.util.concurrent.ExecutionException;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/**
 * The OperationExceptionHandler class handles exceptions related to operations that could not be
 * performed.
 */
@Slf4j
@ControllerAdvice
public class OperationExceptionHandler {
  /**
   * Handles the ExecutionException and UnsupportedOperationException and returns a ResponseEntity
   * with a bad request status.
   *
   * @param ex the Throwable representing the exception that was raised
   * @param request the HttpServletRequest associated with the exception
   * @return a ResponseEntity containing an ErrorResponse and HTTP status code BAD_REQUEST
   */
  @ExceptionHandler(value = {ExecutionException.class, UnsupportedOperationException.class})
  public ResponseEntity<ErrorResponse> handleExecutionException(
      Throwable ex, HttpServletRequest request) {
    log.error("Could not perform an operation", ex);
    return new ResponseEntity<ErrorResponse>(
        ErrorResponse.builder().reason("could not perform operation").build(),
        HttpStatus.BAD_REQUEST);
  }
}
