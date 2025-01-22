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

package com.asc.registration.application.security.filter;

import com.asc.registration.application.security.authentication.BasicSignatureToken;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.HttpStatus;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.web.authentication.WebAuthenticationDetailsSource;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * Filter for authenticating requests using a basic signature token. This filter checks the presence
 * of a signature header in the HTTP request and attempts to authenticate the token using the
 * provided {@link AuthenticationManager}. If authentication succeeds, the security context is
 * updated; otherwise, the request is rejected.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class BasicSignatureAuthenticationFilter extends OncePerRequestFilter {
  private static final String SIGNATURE_HEADER = "X-Signature";
  private final AuthenticationManager authenticationManager;

  /**
   * Performs filtering of incoming HTTP requests to authenticate requests containing the signature
   * header.
   *
   * <p>If the {@code X-Signature} header is present in the request, the token is validated and
   * authenticated using the {@link AuthenticationManager}. On successful authentication, the
   * security context is updated with the authenticated token. If authentication fails, the response
   * is set to {@code 403 Forbidden}.
   *
   * @param request the incoming HTTP request
   * @param response the HTTP response to be sent
   * @param chain the filter chain to pass the request and response to the next filter
   * @throws ServletException if an error occurs during the filtering process
   * @throws IOException if an I/O error occurs
   */
  protected void doFilterInternal(
      HttpServletRequest request, HttpServletResponse response, FilterChain chain)
      throws ServletException, IOException {
    var token = request.getHeader(SIGNATURE_HEADER);
    if (token == null || token.isBlank()) {
      chain.doFilter(request, response);
      return;
    }

    try {
      MDC.put("request_uri", request.getRequestURI());
      log.debug("Validating user");

      var authentication = new BasicSignatureToken(token);
      authentication.setDetails(new WebAuthenticationDetailsSource().buildDetails(request));

      var authenticated = authenticationManager.authenticate(authentication);

      if (authenticated.isAuthenticated()) {
        SecurityContextHolder.getContext().setAuthentication(authenticated);
      }

      chain.doFilter(request, response);
    } catch (BadCredentialsException ex) {
      log.warn("Authentication failed: {}", ex.getMessage());
      response.setStatus(HttpStatus.FORBIDDEN.value());
    } finally {
      MDC.clear();
    }
  }
}
