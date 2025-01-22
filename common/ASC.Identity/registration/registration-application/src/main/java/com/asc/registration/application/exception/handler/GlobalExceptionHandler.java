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
