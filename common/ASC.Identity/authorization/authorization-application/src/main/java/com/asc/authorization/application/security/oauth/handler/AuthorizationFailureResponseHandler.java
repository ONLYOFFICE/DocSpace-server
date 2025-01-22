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

package com.asc.authorization.application.security.oauth.handler;

import com.asc.authorization.application.configuration.properties.SecurityConfigurationProperties;
import com.asc.authorization.application.exception.authorization.AuthorizationProcessingException;
import com.asc.authorization.application.security.SecurityUtils;
import com.asc.authorization.application.security.oauth.error.AuthenticationError;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationCodeRequestAuthenticationException;
import org.springframework.security.web.authentication.AuthenticationFailureHandler;
import org.springframework.stereotype.Component;
import org.springframework.util.StringUtils;
import org.springframework.web.util.UriComponentsBuilder;
import org.springframework.web.util.UriUtils;

/**
 * Handles authentication failures for OAuth2 authorization requests.
 *
 * <p>This handler processes authentication failures, builds error response URIs with appropriate
 * error details, and redirects or sets headers for error responses based on the request context.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AuthorizationFailureResponseHandler implements AuthenticationFailureHandler {
  private final SecurityConfigurationProperties filterSecurityConfigurationProperties;
  private final SecurityUtils securityUtils;

  /**
   * Handles authentication failure by building a redirect URI or setting an error response header.
   *
   * <p>Depending on the type of authentication exception, this method determines the appropriate
   * error code, constructs a redirect URI with the error details, and either performs a redirect or
   * sends the error response headers.
   *
   * @param request the {@link HttpServletRequest}.
   * @param response the {@link HttpServletResponse}.
   * @param exception the {@link AuthenticationException} that caused the failure.
   * @throws IOException if an I/O error occurs during response handling.
   * @throws ServletException if the request cannot be handled.
   */
  public void onAuthenticationFailure(
      HttpServletRequest request, HttpServletResponse response, AuthenticationException exception)
      throws IOException, ServletException {
    log.debug("Authentication failure", exception);

    var clientId = request.getParameter("client_id");
    if (!StringUtils.hasText(clientId)) clientId = "unknown";

    if (exception instanceof AuthorizationProcessingException apException) {
      securityUtils.redirectWithError(
          request, response, clientId, apException.getError().getErrorCode());
      return;
    }

    if (!(exception
        instanceof OAuth2AuthorizationCodeRequestAuthenticationException authException)) {
      securityUtils.redirectWithError(
          request,
          response,
          clientId,
          AuthenticationError.AUTHENTICATION_NOT_SUPPORTED_ERROR.getCode());
      return;
    }

    var error = authException.getError();
    var authRequest = authException.getAuthorizationCodeRequestAuthentication();

    if (authRequest != null && !StringUtils.hasText(authRequest.getRedirectUri())) {
      securityUtils.redirectWithError(
          request, response, clientId, AuthenticationError.INVALID_REDIRECT_URI_ERROR.getCode());
      return;
    }

    if (authRequest == null) {
      securityUtils.redirectWithError(request, response, clientId, error.getErrorCode());
      return;
    }

    var uriBuilder =
        UriComponentsBuilder.fromUriString(authRequest.getRedirectUri())
            .queryParam(OAuth2ParameterNames.ERROR, error.getErrorCode());

    if (StringUtils.hasText(error.getUri()))
      uriBuilder.queryParam(
          OAuth2ParameterNames.ERROR_URI, UriUtils.encode(error.getUri(), StandardCharsets.UTF_8));

    if (StringUtils.hasText(authRequest.getState()))
      uriBuilder.queryParam(
          OAuth2ParameterNames.STATE,
          UriUtils.encode(authRequest.getState(), StandardCharsets.UTF_8));

    var redirectUri = uriBuilder.build(true).toUriString();
    response.setStatus(HttpStatus.OK.value());
    response.setHeader(filterSecurityConfigurationProperties.getRedirectHeader(), redirectUri);

    if (request.getHeader(filterSecurityConfigurationProperties.getDisableRedirectHeader())
        != null) {
      log.debug(
          "Disabling redirect with error code, setting header {} with redirect URL",
          filterSecurityConfigurationProperties.getRedirectHeader());
      response.setStatus(HttpStatus.OK.value());
      response.setHeader(filterSecurityConfigurationProperties.getRedirectHeader(), redirectUri);
    } else {
      log.debug("Redirecting to URL with an error code: {}", redirectUri);
      response.sendRedirect(redirectUri);
    }
  }
}
