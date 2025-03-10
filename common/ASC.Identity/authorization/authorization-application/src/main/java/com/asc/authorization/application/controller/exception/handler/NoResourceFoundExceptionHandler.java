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

import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.web.ErrorResponse;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.servlet.resource.NoResourceFoundException;

/**
 * Handles NoResourceFoundException globally for the application.
 *
 * <p>This exception occurs when a request is made to a non-existing API endpoint. Spring Boot 3.2+
 * throws a {@link NoResourceFoundException} when no mapped resource is found. This handler catches
 * the exception and returns a standardized error response.
 */
@Slf4j
@ControllerAdvice
public class NoResourceFoundExceptionHandler {

  /**
   * Handles the {@link NoResourceFoundException} and returns an error response with HTTP 404
   * status.
   *
   * <p>This method ensures that clients receive a structured error message when requesting a
   * non-existent endpoint.
   *
   * @param ex the {@code NoResourceFoundException} that was raised when the requested resource was
   *     not found.
   * @return an {@code ErrorResponse} containing the error details and HTTP status code {@code
   *     NOT_FOUND}.
   */
  @ExceptionHandler(value = {NoResourceFoundException.class})
  @ResponseStatus(HttpStatus.NOT_FOUND)
  public ErrorResponse handleNoResourceFoundException(NoResourceFoundException ex) {
    return ErrorResponse.create(ex, HttpStatus.NOT_FOUND, ex.getMessage());
  }
}
