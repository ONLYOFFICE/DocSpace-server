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
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationCodeRequestAuthenticationToken;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.stereotype.Component;

/**
 * Handles successful authentication for OAuth2 authorization requests.
 *
 * <p>This handler processes successful authorization requests, constructs a redirect URI with the
 * authorization code, and either performs a redirect or sets the redirect URL in the response
 * header based on the request context.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AuthorizationSuccessResponseHandler implements AuthenticationSuccessHandler {
  private final SecurityConfigurationProperties filterSecurityConfigurationProperties;

  /**
   * Handles successful authentication events.
   *
   * <p>If the authentication object is an instance of {@link
   * OAuth2AuthorizationCodeRequestAuthenticationToken}, it delegates processing to {@link
   * #handleOAuth2Success(HttpServletRequest, HttpServletResponse,
   * OAuth2AuthorizationCodeRequestAuthenticationToken)}. Otherwise, it returns an error response
   * indicating unsupported authentication type.
   *
   * @param request the {@link HttpServletRequest}.
   * @param response the {@link HttpServletResponse}.
   * @param authentication the {@link Authentication} object containing authentication details.
   * @throws IOException if an I/O error occurs during response handling.
   * @throws ServletException if the request cannot be handled.
   */
  public void onAuthenticationSuccess(
      HttpServletRequest request, HttpServletResponse response, Authentication authentication)
      throws IOException, ServletException {
    log.debug("Authorization success");

    if (authentication instanceof OAuth2AuthorizationCodeRequestAuthenticationToken token) {
      handleOAuth2Success(request, response, token);
    } else {
      log.warn(
          "Authentication object is not of type OAuth2AuthorizationCodeRequestAuthenticationToken");
      response.sendError(
          HttpStatus.INTERNAL_SERVER_ERROR.value(), "Authentication type is not supported");
    }
  }

  /**
   * Handles successful OAuth2 authorization.
   *
   * <p>This method constructs a redirect URI with the authorization code and optional state
   * parameter. It either redirects the client to the constructed URI or sets the redirect URI in
   * the response header if redirection is disabled.
   *
   * @param request the {@link HttpServletRequest}.
   * @param response the {@link HttpServletResponse}.
   * @param token the {@link OAuth2AuthorizationCodeRequestAuthenticationToken}.
   * @throws IOException if an I/O error occurs during response handling.
   */
  private void handleOAuth2Success(
      HttpServletRequest request,
      HttpServletResponse response,
      OAuth2AuthorizationCodeRequestAuthenticationToken token)
      throws IOException {
    log.debug("Handling successful authorization for client: {}", token.getClientId());

    var state = token.getState();
    var redirectUri = token.getRedirectUri();
    var authorizationCode = token.getAuthorizationCode();

    if (authorizationCode == null) {
      log.error("Authorization code is null");
      response.sendError(HttpStatus.INTERNAL_SERVER_ERROR.value(), "Authorization code is missing");
      return;
    }

    var authorizationCodeValue = authorizationCode.getTokenValue();
    var redirectUrl =
        new StringBuilder(String.format("%s?code=%s", redirectUri, authorizationCodeValue));

    if (state != null && !state.isBlank()) redirectUrl.append(String.format("&state=%s", state));

    if (request.getHeader(filterSecurityConfigurationProperties.getDisableRedirectHeader())
        != null) {
      log.debug(
          "Disabling redirect, setting header {} with redirect URL",
          filterSecurityConfigurationProperties.getRedirectHeader());
      response.setStatus(HttpStatus.OK.value());
      response.setHeader(
          filterSecurityConfigurationProperties.getRedirectHeader(), redirectUrl.toString());
    } else {
      log.debug("Redirecting to URL: {}", redirectUrl);
      response.sendRedirect(redirectUrl.toString());
    }
  }
}
