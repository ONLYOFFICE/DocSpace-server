package com.asc.registration.application.exception.handler;

import com.asc.registration.application.transfer.ErrorResponse;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/** This class handles all unhandled exceptions and provides a generic error response. */
@Slf4j
@ControllerAdvice
public class UnhandledExceptionHandler {

  /**
   * This method handles all types of exceptions and returns a generic error response.
   *
   * @param ex The exception to be handled.
   * @param request The HTTP servlet request.
   * @return A ResponseEntity containing an ErrorResponse object and a HTTP 500 status.
   */
  @ExceptionHandler(Exception.class)
  public final ResponseEntity<ErrorResponse> handleAllExceptions(
      Exception ex, HttpServletRequest request) {
    log.error("Could not perform an action. Unknown exception", ex);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason("Something went wrong").build(),
        HttpStatus.INTERNAL_SERVER_ERROR);
  }
}
