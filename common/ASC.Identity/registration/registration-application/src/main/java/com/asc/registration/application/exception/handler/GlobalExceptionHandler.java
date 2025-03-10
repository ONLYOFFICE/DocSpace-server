// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.application.exception.handler;

import com.asc.common.core.domain.exception.DomainNotFoundException;
import com.asc.registration.application.transfer.ErrorResponse;
import com.asc.registration.core.domain.exception.ClientDomainException;
import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import io.grpc.StatusRuntimeException;
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
import org.springframework.security.authorization.AuthorizationDeniedException;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.method.annotation.HandlerMethodValidationException;
import org.springframework.web.servlet.resource.NoResourceFoundException;

/**
 * GlobalExceptionHandler is a centralized exception handler that intercepts and processes various
 * exceptions thrown during application execution. Each handler method constructs a standardized
 * {@link ErrorResponse} and returns a {@link ResponseEntity} with an appropriate HTTP status code.
 */
@Slf4j
@ControllerAdvice
public class GlobalExceptionHandler {
  /**
   * Handles {@link RequestNotPermitted} exceptions thrown when the rate limiter blocks a request.
   *
   * <p>This method returns a response with an HTTP status of {@code 429 Too Many Requests} and an
   * error message indicating that too many requests have been made.
   *
   * @param ex the {@link RequestNotPermitted} exception that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ResponseEntity} containing an {@link ErrorResponse} with a "too many requests"
   *     message.
   */
  @ExceptionHandler(value = RequestNotPermitted.class)
  public ResponseEntity<ErrorResponse> handleRequestNotPermitted(
      RequestNotPermitted ex, HttpServletRequest request) {
    return new ResponseEntity<>(
        ErrorResponse.builder().reason("too many requests").build(), HttpStatus.TOO_MANY_REQUESTS);
  }

  /**
   * Handles {@link NoResourceFoundException} exceptions thrown when a request targets a
   * non-existing API endpoint.
   *
   * <p>Spring Boot 3.2+ throws a {@link NoResourceFoundException} for such cases. This handler
   * returns an error response with an HTTP status of {@code 404 Not Found}.
   *
   * @param ex the {@link NoResourceFoundException} that was raised.
   * @return an {@link org.springframework.web.ErrorResponse} encapsulating the error details and a
   *     {@code 404 Not Found} status.
   */
  @ExceptionHandler(value = {NoResourceFoundException.class})
  @ResponseStatus(HttpStatus.NOT_FOUND)
  public org.springframework.web.ErrorResponse handleNoResourceFoundException(
      NoResourceFoundException ex) {
    return org.springframework.web.ErrorResponse.create(ex, HttpStatus.NOT_FOUND, ex.getMessage());
  }

  /**
   * Handles {@link MethodArgumentNotValidException} exceptions resulting from failed validation of
   * method arguments.
   *
   * <p>This method extracts and concatenates all default error messages from the validation errors
   * and returns an error response with an HTTP status of {@code 400 Bad Request}.
   *
   * @param e the {@link MethodArgumentNotValidException} containing validation error details.
   * @return a {@link ResponseEntity} with an {@link ErrorResponse} that includes the concatenated
   *     validation error messages.
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
   * Handles {@link HandlerMethodValidationException} exceptions arising from method-level
   * validation failures.
   *
   * <p>This method aggregates all resolvable error messages from the validation results and returns
   * a response with an HTTP status of {@code 400 Bad Request}.
   *
   * @param e the {@link HandlerMethodValidationException} containing details about the validation
   *     errors.
   * @return a {@link ResponseEntity} with an {@link ErrorResponse} that includes the concatenated
   *     error messages.
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
   * Handles {@link ValidationException} exceptions thrown during validation processes.
   *
   * <p>This handler returns an error response with an HTTP status of {@code 400 Bad Request} that
   * includes the exception's message.
   *
   * @param e the {@link ValidationException} that was raised.
   * @return a {@link ResponseEntity} containing an {@link ErrorResponse} with the exception's
   *     message.
   */
  @ExceptionHandler(value = ValidationException.class)
  public ResponseEntity<?> handleValidationException(ValidationException e) {
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(e.getMessage()).build(), HttpStatus.BAD_REQUEST);
  }

  /**
   * Handles {@link ClientDomainException} exceptions indicating issues on the client-side domain
   * logic.
   *
   * <p>This method logs a warning and returns an error response with an HTTP status of {@code 400
   * Bad Request}.
   *
   * @param ex the {@link ClientDomainException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ResponseEntity} containing an {@link ErrorResponse} with details of the client
   *     domain error.
   */
  @ExceptionHandler(ClientDomainException.class)
  public ResponseEntity<ErrorResponse> handleClientDomainException(
      ClientDomainException ex, HttpServletRequest request) {
    log.warn("Client domain exception has been raised", ex);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(ex.getMessage()).build(), HttpStatus.BAD_REQUEST);
  }

  /**
   * Handles {@link DataAccessException} exceptions that occur during database operations.
   *
   * <p>This method logs the error and returns an empty response with an HTTP status of {@code 500
   * Internal Server Error}.
   *
   * @param ex the {@link DataAccessException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ResponseEntity} with HTTP status {@code 500 Internal Server Error}.
   */
  @ExceptionHandler(DataAccessException.class)
  public ResponseEntity<?> handleDataAccessException(
      DataAccessException ex, HttpServletRequest request) {
    log.error("Could not perform a database operation", ex);
    return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
  }

  /**
   * Handles {@link DomainNotFoundException} exceptions thrown when a requested domain entity is not
   * found.
   *
   * <p>This method logs a warning and returns an error response with an HTTP status of {@code 404
   * Not Found}.
   *
   * @param ex the {@link DomainNotFoundException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ResponseEntity} containing an {@link ErrorResponse} with a message indicating
   *     the domain was not found.
   */
  @ExceptionHandler(DomainNotFoundException.class)
  public ResponseEntity<ErrorResponse> handleDomainNotFound(
      DomainNotFoundException ex, HttpServletRequest request) {
    log.warn("Could not find a domain", ex);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason(ex.getMessage()).build(), HttpStatus.NOT_FOUND);
  }

  /**
   * Handles exceptions of type {@link ExecutionException} and {@link UnsupportedOperationException}
   * which may occur during asynchronous execution or unsupported operations.
   *
   * <p>This method logs the error and returns a generic error response with an HTTP status of
   * {@code 400 Bad Request}.
   *
   * @param ex the {@link Throwable} representing the exception that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ResponseEntity} containing an {@link ErrorResponse} with a message indicating
   *     that the operation could not be performed.
   */
  @ExceptionHandler(value = {ExecutionException.class, UnsupportedOperationException.class})
  public ResponseEntity<ErrorResponse> handleExecutionException(
      Throwable ex, HttpServletRequest request) {
    log.error("Could not perform an operation", ex);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason("could not perform operation").build(),
        HttpStatus.BAD_REQUEST);
  }

  /**
   * Handles {@link AuthorizationDeniedException} exceptions that occur when access to a resource is
   * denied.
   *
   * <p>This method returns an error response with an HTTP status of {@code 403 Forbidden} and a
   * message indicating access denial.
   *
   * @param e the {@link AuthorizationDeniedException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ResponseEntity} containing an {@link ErrorResponse} with an "access denied"
   *     message.
   */
  @ExceptionHandler(value = AuthorizationDeniedException.class)
  public ResponseEntity<ErrorResponse> handleAccessDeniedException(
      AuthorizationDeniedException e, HttpServletRequest request) {
    return new ResponseEntity<>(
        ErrorResponse.builder().reason("access denied").build(), HttpStatus.FORBIDDEN);
  }

  /**
   * Handles {@link StatusRuntimeException} exceptions that occur during gRPC service
   * communications.
   *
   * <p>This method inspects the exception message for the keyword "unavailable" (case-insensitive)
   * to determine if the error is due to a service being unavailable. It returns a response with an
   * HTTP status of {@code 503 Service Unavailable} if the service is unavailable, or {@code 500
   * Internal Server Error} otherwise.
   *
   * @param e the {@link StatusRuntimeException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ResponseEntity} containing an {@link ErrorResponse} with a message indicating
   *     either service unavailability or a generic error.
   */
  @ExceptionHandler(value = StatusRuntimeException.class)
  public ResponseEntity<ErrorResponse> handleServicesCommunicationException(
      StatusRuntimeException e, HttpServletRequest request) {
    var isUnavailable =
        e.getMessage() != null && e.getMessage().toLowerCase().contains("unavailable");
    return new ResponseEntity<>(
        ErrorResponse.builder()
            .reason(isUnavailable ? "service unavailable" : "something went wrong")
            .build(),
        isUnavailable ? HttpStatus.SERVICE_UNAVAILABLE : HttpStatus.INTERNAL_SERVER_ERROR);
  }

  /**
   * Handles all uncaught exceptions that are not explicitly handled by other methods.
   *
   * <p>This generic fallback handler logs the exception and returns an error response with an HTTP
   * status of {@code 500 Internal Server Error}.
   *
   * @param ex the {@link Exception} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ResponseEntity} containing an {@link ErrorResponse} with a generic error
   *     message.
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
