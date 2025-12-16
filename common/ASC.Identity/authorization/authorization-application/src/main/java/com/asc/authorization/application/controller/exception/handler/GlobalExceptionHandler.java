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

package com.asc.authorization.application.controller.exception.handler;

import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import jakarta.servlet.http.HttpServletRequest;
import java.net.URI;
import lombok.extern.slf4j.Slf4j;
import org.springframework.dao.DataAccessException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ProblemDetail;
import org.springframework.web.HttpRequestMethodNotSupportedException;
import org.springframework.web.bind.MissingServletRequestParameterException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.servlet.resource.NoResourceFoundException;

/**
 * GlobalExceptionHandler is a centralized exception handler that intercepts and processes various
 * exceptions thrown during application execution. Each handler method returns a standardized RFC
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
   * Handles the {@link NoResourceFoundException} and returns an error response with HTTP 404
   * status.
   *
   * @param ex the {@code NoResourceFoundException} that was raised when the requested resource was
   *     not found.
   * @param request the {@link HttpServletRequest} associated with the current request.
   * @return a {@link ProblemDetail} containing the error details and HTTP status code {@code
   *     NOT_FOUND}.
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
   * Handles {@link DataAccessException} and returns a response with HTTP status 500.
   *
   * @param ex the {@link DataAccessException} thrown during a database operation
   * @param request the {@link HttpServletRequest} in which the exception was raised
   * @return a {@link ProblemDetail} with HTTP status 500 (Internal Server Error)
   */
  @ExceptionHandler(DataAccessException.class)
  public ProblemDetail handleDataAccessException(
      DataAccessException ex, HttpServletRequest request) {
    log.error("Could not perform a database operation", ex);
    return createProblemDetail(
        HttpStatus.INTERNAL_SERVER_ERROR, "A database error occurred", request.getRequestURI());
  }

  /**
   * Handles {@link RequestNotPermitted} exceptions, which are thrown when a request is not
   * permitted by the rate limiter.
   *
   * @param ex the exception that was thrown.
   * @param request the {@link HttpServletRequest} that resulted in the exception.
   * @return a {@link ProblemDetail} with status code 429 (Too Many Requests).
   */
  @ExceptionHandler(value = {RequestNotPermitted.class})
  public ProblemDetail handleRequestNotPermitted(Throwable ex, HttpServletRequest request) {
    return createProblemDetail(
        HttpStatus.TOO_MANY_REQUESTS,
        "Too many requests. Please try again later.",
        request.getRequestURI());
  }

  /**
   * Handles any {@link Exception} and returns a response with HTTP status 500.
   *
   * @param ex the {@link Exception} thrown during an operation
   * @param request the {@link HttpServletRequest} in which the exception was raised
   * @return a {@link ProblemDetail} with HTTP status 500 (Internal Server Error)
   */
  @ExceptionHandler(Exception.class)
  public ProblemDetail handleAllExceptions(Exception ex, HttpServletRequest request) {
    log.error("Could not perform an action. Unknown exception", ex);
    return createProblemDetail(
        HttpStatus.INTERNAL_SERVER_ERROR, "Something went wrong", request.getRequestURI());
  }
}
