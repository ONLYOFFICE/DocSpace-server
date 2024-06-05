package com.asc.registration.application.exception.handler;

import com.asc.common.core.domain.exception.DomainNotFoundException;
import com.asc.registration.application.transfer.ErrorResponse;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/**
 * The DomainNotFoundExceptionHandler class handles exceptions related to domain not found
 * scenarios.
 */
@Slf4j
@ControllerAdvice
public class DomainNotFoundExceptionHandler {
  /**
   * Handles the DomainNotFoundException and returns a ResponseEntity with a not found status.
   *
   * @param ex the DomainNotFoundException that was raised
   * @param request the HttpServletRequest associated with the exception
   * @return a ResponseEntity containing an ErrorResponse and HTTP status code NOT_FOUND
   */
  @ExceptionHandler(DomainNotFoundException.class)
  public ResponseEntity<ErrorResponse> handleDomainNotFound(
      DomainNotFoundException ex, HttpServletRequest request) {
    log.warn("Could not find a domain", ex);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(ex.getMessage()).build(), HttpStatus.NOT_FOUND);
  }
}
