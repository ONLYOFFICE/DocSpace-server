package com.asc.registration.application.exception.handler;

import com.asc.registration.application.transfer.ErrorResponse;
import com.asc.registration.core.domain.exception.ClientDomainException;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/** The ClientDomainExceptionHandler class handles exceptions specific to the client domain. */
@Slf4j
@ControllerAdvice
public class ClientDomainExceptionHandler {
  /**
   * Handles the ClientDomainException and returns a ResponseEntity with an error response.
   *
   * @param ex the ClientDomainException that was raised
   * @param request the HttpServletRequest associated with the exception
   * @return a ResponseEntity containing an ErrorResponse and HTTP status code
   */
  @ExceptionHandler(ClientDomainException.class)
  public ResponseEntity<ErrorResponse> handleClientDomainException(
      ClientDomainException ex, HttpServletRequest request) {
    log.warn("Client domain exception has been raised", ex);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(ex.getMessage()).build(), HttpStatus.BAD_REQUEST);
  }
}
