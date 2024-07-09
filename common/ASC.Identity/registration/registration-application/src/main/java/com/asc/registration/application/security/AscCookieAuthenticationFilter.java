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

package com.asc.registration.application.security;

import com.asc.common.application.transfer.response.AscPersonResponse;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.regex.Pattern;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * AscCookieAuthenticationFilter is a Spring Security filter that processes requests to validate
 * users based on ASC authentication cookies.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AscCookieAuthenticationFilter extends OncePerRequestFilter {
  @Value("${web.api}")
  private String webApi;

  private final AscCookieAuthenticationProcessor ascCookieAuthenticationProcessor;

  /**
   * Filters the request to validate users.
   *
   * @param request the HTTP request
   * @param response the HTTP response
   * @param chain the filter chain
   * @throws ServletException in case of servlet errors
   * @throws IOException in case of I/O errors
   */
  protected void doFilterInternal(
      HttpServletRequest request, HttpServletResponse response, FilterChain chain)
      throws ServletException, IOException {
    MDC.put("request_uri", request.getRequestURI());
    log.debug("Validating user");

    try {
      ascCookieAuthenticationProcessor.processAscCookies(request);
      var attribute = request.getAttribute("person");
      if (attribute instanceof AscPersonResponse me) {
        if (me.getIsOwner() || me.getIsAdmin() || isUserAllowedPath(request.getRequestURI())) {
          chain.doFilter(request, response);
        } else {
          log.warn("User is not authorized to access this endpoint");
          response.setStatus(HttpStatus.FORBIDDEN.value());
        }
      } else {
        response.setStatus(HttpStatus.UNAUTHORIZED.value());
      }
    } catch (BadCredentialsException accessException) {
      log.warn("Authentication failed: {}", accessException.getMessage());
      response.setStatus(HttpStatus.UNAUTHORIZED.value());
    } finally {
      MDC.clear();
    }
  }

  /**
   * Determines if the filter should not be applied to a given request.
   *
   * @param request the HTTP request
   * @return true if the request should be excluded from filtering, false otherwise
   */
  protected boolean shouldNotFilter(HttpServletRequest request) {
    var path = request.getRequestURI();
    return isExcludedPath(path);
  }

  private boolean isExcludedPath(String path) {
    return Pattern.matches(String.format("%s/oauth/info", webApi), path)
        || Pattern.matches("/health(/.*)?", path)
        || Pattern.matches(String.format("%s/clients/.*/public/info", webApi), path);
  }

  private boolean isUserAllowedPath(String path) {
    return Pattern.matches(String.format("%s/scopes", webApi), path)
        || Pattern.matches(String.format("%s/clients/.*?/info", webApi), path)
        || Pattern.matches(String.format("%s/clients/info", webApi), path)
        || Pattern.matches(String.format("%s/clients/consents", webApi), path);
  }
}
