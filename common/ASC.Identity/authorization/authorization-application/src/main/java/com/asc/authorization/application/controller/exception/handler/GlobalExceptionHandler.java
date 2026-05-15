// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
@ControllerAdvice("authorizationGlobalExceptionHandler")
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
