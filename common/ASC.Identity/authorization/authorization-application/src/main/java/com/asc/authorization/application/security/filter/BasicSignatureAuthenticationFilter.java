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

package com.asc.authorization.application.security.filter;

import com.asc.authorization.application.configuration.properties.SecurityConfigurationProperties;
import com.asc.authorization.application.exception.authentication.AuthenticationProcessingException;
import com.asc.authorization.application.exception.client.RegisteredClientPermissionException;
import com.asc.authorization.application.security.SecurityUtils;
import com.asc.authorization.application.security.oauth.error.AuthenticationError;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.regex.Pattern;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpMethod;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * Filter for handling authentication based on a signature and replacing anonymous authentication
 * with actual authentication.
 *
 * <p>This filter intercepts requests to specific OAuth2 endpoints, validates the client ID and
 * signature, and sets the authenticated user in the security context. Errors during processing
 * result in redirections with appropriate error codes.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class BasicSignatureAuthenticationFilter extends OncePerRequestFilter {
  /** Pattern for matching the OAuth2 authorization endpoint. */
  private static final Pattern AUTHORIZE_PATTERN = Pattern.compile("/oauth2/authorize");

  /** Pattern for matching the OAuth2 login endpoint. */
  private static final Pattern LOGIN_PATTERN = Pattern.compile("/oauth2/login");

  private final SecurityUtils securityUtils;
  private final AuthenticationManager authenticationManager;
  private final SecurityConfigurationProperties securityConfigProperties;

  /**
   * Filters incoming requests, replacing anonymous authentication with actual authentication.
   *
   * <p>This method validates the presence of a client ID and signature, performs authentication,
   * and sets the security context for successfully authenticated requests. In case of errors, it
   * redirects the user with an appropriate error message.
   *
   * @param request the {@link HttpServletRequest}.
   * @param response the {@link HttpServletResponse}.
   * @param chain the {@link FilterChain}.
   * @throws ServletException if an error occurs during the filter process.
   * @throws IOException if an I/O error occurs during the filter process.
   */
  protected void doFilterInternal(
      HttpServletRequest request, HttpServletResponse response, FilterChain chain)
      throws ServletException, IOException {
    var clientId = request.getParameter(securityConfigProperties.getClientIdParameter());

    if (clientId == null || clientId.isBlank()) {
      log.warn(
          "Missing or empty '{}' in query string", securityConfigProperties.getClientIdParameter());
      securityUtils.redirectWithError(
          request, response, clientId, AuthenticationError.MISSING_CLIENT_ID_ERROR.getCode());
      return;
    }

    var signature = request.getHeader(securityConfigProperties.getSignatureHeader());
    if (signature == null || signature.isBlank()) {
      log.warn("Missing '{}' signature header", securityConfigProperties.getSignatureHeader());
      securityUtils.redirectWithError(
          request, response, clientId, AuthenticationError.MISSING_ASC_SIGNATURE.getCode());
      return;
    }

    try {
      var authenticationToken = new UsernamePasswordAuthenticationToken(clientId, signature);
      var authentication = authenticationManager.authenticate(authenticationToken);
      if (authentication.isAuthenticated()) {
        SecurityContextHolder.getContext().setAuthentication(authentication);
        securityUtils.setSecurityHeaders(response);
      }

      chain.doFilter(request, response);
    } catch (RegisteredClientPermissionException pex) {
      log.warn("Current client does not permit current operation", pex);
      securityUtils.redirectWithError(
          request,
          response,
          clientId,
          AuthenticationError.CLIENT_PERMISSION_DENIED_ERROR.getCode());
    } catch (AuthenticationProcessingException aex) {
      log.warn("Could not process authentication", aex);
      securityUtils.redirectWithError(request, response, clientId, aex.getError().getCode());
    } catch (Exception e) {
      log.warn("Something went wrong", e);
      securityUtils.redirectWithError(
          request, response, clientId, AuthenticationError.SOMETHING_WENT_WRONG_ERROR.getCode());
    }
  }

  /**
   * Determines whether the filter should not be applied to a given request.
   *
   * <p>This method excludes requests to non-relevant endpoints from being processed by this filter.
   *
   * @param request the {@link HttpServletRequest}.
   * @return {@code true} if the filter should not be applied, {@code false} otherwise.
   */
  protected boolean shouldNotFilter(HttpServletRequest request) {
    var path = request.getRequestURI();
    return !(LOGIN_PATTERN.matcher(path).find() && HttpMethod.POST.matches(request.getMethod()))
        && !(AUTHORIZE_PATTERN.matcher(path).find());
  }
}
