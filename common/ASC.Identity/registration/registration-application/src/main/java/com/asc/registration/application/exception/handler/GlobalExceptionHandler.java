// (c) Copyright Ascensio System SIA 2009-2026
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
import com.asc.registration.core.domain.exception.ClientDomainException;
import com.asc.registration.service.exception.ExceededClientsPerResourceException;
import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import io.grpc.StatusRuntimeException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.ValidationException;
import java.net.URI;
import java.util.concurrent.ExecutionException;
import java.util.stream.Collectors;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.MessageSourceResolvable;
import org.springframework.context.support.DefaultMessageSourceResolvable;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ProblemDetail;
import org.springframework.security.authorization.AuthorizationDeniedException;
import org.springframework.web.HttpMediaTypeNotSupportedException;
import org.springframework.web.HttpRequestMethodNotSupportedException;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.MissingServletRequestParameterException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.method.annotation.HandlerMethodValidationException;
import org.springframework.web.servlet.resource.NoResourceFoundException;

/**
 * GlobalExceptionHandler is a centralized exception handler that intercepts and processes various
 * exceptions thrown during application execution. Each handler method constructs a standardized RFC
 * 7807 {@link ProblemDetail} response.
 */
@Slf4j
@ControllerAdvice
public class GlobalExceptionHandler {
  private static final URI ERROR_TYPE_URI =
      URI.create("https://api.onlyoffice.com/docspace/api-backend/get-started/basic-concepts");

  /**
   * Creates a ProblemDetail with the standard type URI and instance path.
   *
   * @param status the HTTP status
   * @param detail the detail message
   * @param instance the request URI
   * @return a ProblemDetail instance
   */
  private ProblemDetail createProblemDetail(HttpStatus status, String detail, String instance) {
    var problemDetail = ProblemDetail.forStatusAndDetail(status, detail);
    problemDetail.setType(ERROR_TYPE_URI);
    problemDetail.setInstance(URI.create(instance));
    return problemDetail;
  }

  /**
   * Handles {@link NoResourceFoundException} exceptions thrown when a request targets a
   * non-existing API endpoint.
   *
   * @param ex the {@link NoResourceFoundException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with {@code 404 Not Found} status.
   */
  @ExceptionHandler(value = {NoResourceFoundException.class})
  public ProblemDetail handleNoResourceFoundException(
      NoResourceFoundException ex, HttpServletRequest request) {
    return createProblemDetail(HttpStatus.NOT_FOUND, ex.getMessage(), request.getRequestURI());
  }

  /**
   * Handles {@link HttpRequestMethodNotSupportedException} and returns a response with HTTP status
   * 405.
   *
   * @param ex the {@link HttpRequestMethodNotSupportedException} thrown when an unsupported HTTP
   *     method is used
   * @param request the {@link HttpServletRequest} in which the exception was raised
   * @return a {@link ProblemDetail} with HTTP status 405 (Method Not Allowed)
   */
  @ExceptionHandler(HttpRequestMethodNotSupportedException.class)
  public ProblemDetail handleMethodNotSupportedException(
      HttpRequestMethodNotSupportedException ex, HttpServletRequest request) {
    return createProblemDetail(
        HttpStatus.METHOD_NOT_ALLOWED, ex.getMessage(), request.getRequestURI());
  }

  /**
   * Handles {@link MissingServletRequestParameterException} and returns a response with HTTP status
   * 400.
   *
   * @param ex the {@link MissingServletRequestParameterException} thrown when a required request
   *     parameter is missing
   * @param request the {@link HttpServletRequest} in which the exception was raised
   * @return a {@link ProblemDetail} with HTTP status 400 (Bad Request)
   */
  @ExceptionHandler(MissingServletRequestParameterException.class)
  public ProblemDetail handleMissingParameterException(
      MissingServletRequestParameterException ex, HttpServletRequest request) {
    return createProblemDetail(HttpStatus.BAD_REQUEST, ex.getMessage(), request.getRequestURI());
  }

  /**
   * Handles {@link MethodArgumentNotValidException} exceptions resulting from failed validation of
   * method arguments.
   *
   * @param e the {@link MethodArgumentNotValidException} containing validation error details.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} that includes the concatenated validation error messages.
   */
  @ExceptionHandler(value = {MethodArgumentNotValidException.class})
  public ProblemDetail handleValidationException(
      MethodArgumentNotValidException e, HttpServletRequest request) {
    var errors =
        e.getAllErrors().stream()
            .map(DefaultMessageSourceResolvable::getDefaultMessage)
            .collect(Collectors.joining(", "));
    return createProblemDetail(HttpStatus.BAD_REQUEST, errors, request.getRequestURI());
  }

  /**
   * Handles {@link HandlerMethodValidationException} exceptions arising from method-level
   * validation failures.
   *
   * @param e the {@link HandlerMethodValidationException} containing details about the validation
   *     errors.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} that includes the concatenated error messages.
   */
  @ExceptionHandler(value = HandlerMethodValidationException.class)
  public ProblemDetail handleValidationException(
      HandlerMethodValidationException e, HttpServletRequest request) {
    var errors =
        e.getAllErrors().stream()
            .map(MessageSourceResolvable::getDefaultMessage)
            .collect(Collectors.joining(", "));
    return createProblemDetail(HttpStatus.BAD_REQUEST, errors, request.getRequestURI());
  }

  /**
   * Handles {@link ValidationException} exceptions thrown during validation processes.
   *
   * @param e the {@link ValidationException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with the exception's message.
   */
  @ExceptionHandler(value = ValidationException.class)
  public ProblemDetail handleValidationException(
      ValidationException e, HttpServletRequest request) {
    return createProblemDetail(HttpStatus.BAD_REQUEST, e.getMessage(), request.getRequestURI());
  }

  /**
   * Handles {@link AuthorizationDeniedException} exceptions that occur when access to a resource is
   * denied.
   *
   * @param e the {@link AuthorizationDeniedException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with an "access denied" message.
   */
  @ExceptionHandler(value = AuthorizationDeniedException.class)
  public ProblemDetail handleAccessDeniedException(
      AuthorizationDeniedException e, HttpServletRequest request) {
    return createProblemDetail(HttpStatus.FORBIDDEN, "Access denied", request.getRequestURI());
  }

  /**
   * Handles {@link ExceededClientsPerResourceException} exceptions thrown when a tenant exceeds
   * their client allocation limit.
   *
   * @param e the {@link ExceededClientsPerResourceException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with the exception's message about exceeding the client limit.
   */
  @ExceptionHandler(value = ExceededClientsPerResourceException.class)
  public ProblemDetail handleExceededClientsPerResourceException(
      ExceededClientsPerResourceException e, HttpServletRequest request) {
    return createProblemDetail(HttpStatus.BAD_REQUEST, e.getMessage(), request.getRequestURI());
  }

  /**
   * Handles {@link ClientDomainException} exceptions indicating issues on the client-side domain
   * logic.
   *
   * @param ex the {@link ClientDomainException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with details of the client domain error.
   */
  @ExceptionHandler(ClientDomainException.class)
  public ProblemDetail handleClientDomainException(
      ClientDomainException ex, HttpServletRequest request) {
    return createProblemDetail(HttpStatus.BAD_REQUEST, ex.getMessage(), request.getRequestURI());
  }

  /**
   * Handles {@link DomainNotFoundException} exceptions thrown when a requested domain entity is not
   * found.
   *
   * @param ex the {@link DomainNotFoundException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with a message indicating the domain was not found.
   */
  @ExceptionHandler(DomainNotFoundException.class)
  public ProblemDetail handleDomainNotFound(
      DomainNotFoundException ex, HttpServletRequest request) {
    return createProblemDetail(HttpStatus.NOT_FOUND, ex.getMessage(), request.getRequestURI());
  }

  /**
   * Handles {@link DataAccessException} exceptions that occur during database operations.
   *
   * @param ex the {@link DataAccessException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with HTTP status {@code 500 Internal Server Error}.
   */
  @ExceptionHandler(DataAccessException.class)
  public ProblemDetail handleDataAccessException(
      DataAccessException ex, HttpServletRequest request) {
    log.error("Could not perform a database operation", ex);
    return createProblemDetail(
        HttpStatus.INTERNAL_SERVER_ERROR, "A database error occurred", request.getRequestURI());
  }

  /**
   * Handles exceptions of type {@link ExecutionException} and {@link UnsupportedOperationException}
   * which may occur during asynchronous execution or unsupported operations.
   *
   * @param ex the {@link Throwable} representing the exception that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with a message indicating that the operation could not be
   *     performed.
   */
  @ExceptionHandler(value = {ExecutionException.class, UnsupportedOperationException.class})
  public ProblemDetail handleExecutionException(Throwable ex, HttpServletRequest request) {
    log.warn("Could not perform an operation", ex);
    return createProblemDetail(
        HttpStatus.BAD_REQUEST, "Could not perform operation", request.getRequestURI());
  }

  /**
   * Handles {@link StatusRuntimeException} exceptions that occur during gRPC service
   * communications.
   *
   * @param e the {@link StatusRuntimeException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with a message indicating either service unavailability or a
   *     generic error.
   */
  @ExceptionHandler(value = StatusRuntimeException.class)
  public ProblemDetail handleServicesCommunicationException(
      StatusRuntimeException e, HttpServletRequest request) {
    var isUnavailable =
        e.getMessage() != null && e.getMessage().toLowerCase().contains("unavailable");
    if (isUnavailable) {
      return createProblemDetail(
          HttpStatus.SERVICE_UNAVAILABLE, "Service unavailable", request.getRequestURI());
    }
    return createProblemDetail(
        HttpStatus.INTERNAL_SERVER_ERROR, "Something went wrong", request.getRequestURI());
  }

  /**
   * Handles {@link RequestNotPermitted} exceptions thrown when the rate limiter blocks a request.
   *
   * @param ex the {@link RequestNotPermitted} exception that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with a "too many requests" message.
   */
  @ExceptionHandler(value = RequestNotPermitted.class)
  public ProblemDetail handleRequestNotPermitted(
      RequestNotPermitted ex, HttpServletRequest request) {
    return createProblemDetail(
        HttpStatus.TOO_MANY_REQUESTS,
        "Too many requests. Please try again later.",
        request.getRequestURI());
  }

  /**
   * Handles {@link HttpMediaTypeNotSupportedException} exceptions thrown when a request contains a
   * media type that is not supported by the endpoint.
   *
   * @param ex the {@link HttpMediaTypeNotSupportedException} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with the exception's message about the unsupported media type.
   */
  @ExceptionHandler(value = HttpMediaTypeNotSupportedException.class)
  public ProblemDetail handleNotSupportedMediaType(
      HttpMediaTypeNotSupportedException ex, HttpServletRequest request) {
    return createProblemDetail(
        HttpStatus.UNSUPPORTED_MEDIA_TYPE, ex.getMessage(), request.getRequestURI());
  }

  /**
   * Handles all uncaught exceptions that are not explicitly handled by other methods.
   *
   * @param ex the {@link Exception} that was raised.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} with a generic error message.
   */
  @ExceptionHandler(Exception.class)
  public ProblemDetail handleAllExceptions(Exception ex, HttpServletRequest request) {
    log.error("Could not perform an action. Unknown exception", ex);
    return createProblemDetail(
        HttpStatus.INTERNAL_SERVER_ERROR, "Something went wrong", request.getRequestURI());
  }
}
