package com.asc.registration.application.exception.handler;

import com.asc.registration.application.transfer.ErrorResponse;
import jakarta.validation.ValidationException;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/** This class handles validation exceptions and provides a specific error response. */
@Slf4j
@ControllerAdvice
public class ValidationExceptionHandler {

  /**
   * This method handles validation exceptions and returns a specific error response.
   *
   * @param e The validation exception to be handled.
   * @return A ResponseEntity containing an ErrorResponse object and a HTTP 400 status.
   */
  @ExceptionHandler(value = {MethodArgumentNotValidException.class, ValidationException.class})
  public ResponseEntity<ErrorResponse> handleValidationException(Throwable e) {
    log.error("Could not validate a model", e);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(e.getMessage()).build(), HttpStatus.BAD_REQUEST);
  }
}
