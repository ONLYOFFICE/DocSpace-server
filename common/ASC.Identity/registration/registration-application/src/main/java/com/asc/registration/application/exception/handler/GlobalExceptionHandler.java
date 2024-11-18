package com.asc.registration.application.exception.handler;

import com.asc.common.core.domain.exception.DomainNotFoundException;
import com.asc.registration.application.transfer.ErrorResponse;
import com.asc.registration.core.domain.exception.ClientDomainException;
import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.ValidationException;
import java.util.concurrent.ExecutionException;
import java.util.stream.Collectors;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.MessageSourceResolvable;
import org.springframework.context.support.DefaultMessageSourceResolvable;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.method.annotation.HandlerMethodValidationException;

/** The GlobalExceptionHandler class handles exceptions depending on the type. */
@Slf4j
@ControllerAdvice
public class GlobalExceptionHandler {
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
    return new ResponseEntity<>(
        ErrorResponse.builder().reason("too many requests").build(), HttpStatus.TOO_MANY_REQUESTS);
  }

  /**
   * This method handles validation exceptions and returns a specific error response.
   *
   * @param e The validation exception to be handled.
   * @return A ResponseEntity containing an ErrorResponse object and a HTTP 400 status.
   */
  @ExceptionHandler(value = {MethodArgumentNotValidException.class})
  public ResponseEntity<ErrorResponse> handleValidationException(
      MethodArgumentNotValidException e) {
    var errors =
        e.getAllErrors().stream()
            .map(DefaultMessageSourceResolvable::getDefaultMessage)
            .collect(Collectors.joining(", "));
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(errors).build(), HttpStatus.BAD_REQUEST);
  }

  /**
   * This method handles validation exceptions and returns a specific error response.
   *
   * @param e The validation exception to be handled.
   * @return A ResponseEntity containing an ErrorResponse object and a HTTP 400 status.
   */
  @ExceptionHandler(value = HandlerMethodValidationException.class)
  public ResponseEntity<?> handleValidationException(HandlerMethodValidationException e) {
    var errors =
        e.getAllValidationResults().stream()
            .flatMap(result -> result.getResolvableErrors().stream())
            .map(MessageSourceResolvable::getDefaultMessage)
            .collect(Collectors.joining(", "));
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(errors).build(), HttpStatus.BAD_REQUEST);
  }

  /**
   * This method handles validation exceptions and returns a specific error response.
   *
   * @param e The validation exception to be handled.
   * @return A ResponseEntity containing an ErrorResponse object and a HTTP 400 status.
   */
  @ExceptionHandler(value = ValidationException.class)
  public ResponseEntity<?> handleValidationException(ValidationException e) {
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(e.getMessage()).build(), HttpStatus.BAD_REQUEST);
  }

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
        ErrorResponse.builder().reason("something went wrong").build(),
        HttpStatus.INTERNAL_SERVER_ERROR);
  }
}
