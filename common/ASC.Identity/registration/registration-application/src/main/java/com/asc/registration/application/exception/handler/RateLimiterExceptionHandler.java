// (c) Copyright Ascensio System SIA 2009-2024
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

import com.asc.registration.application.transfer.ErrorResponse;
import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/** The RateLimiterExceptionHandler class handles exceptions related to rate limiting requests. */
@Slf4j
@ControllerAdvice
public class RateLimiterExceptionHandler {
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
    log.warn("Rate limiter has blocked current call", ex);
    return new ResponseEntity<>(
        ErrorResponse.builder().reason("Too many requests").build(), HttpStatus.TOO_MANY_REQUESTS);
  }
}
