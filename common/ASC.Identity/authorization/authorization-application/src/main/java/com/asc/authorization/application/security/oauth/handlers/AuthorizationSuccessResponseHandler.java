package com.asc.authorization.application.security.oauth.handlers;

import com.asc.authorization.application.configuration.security.AnonymousFilterSecurityConfigurationProperties;
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

/** Handles successful authentication for OAuth2 authorization requests. */
@Slf4j
@Component
@RequiredArgsConstructor
public class AuthorizationSuccessResponseHandler implements AuthenticationSuccessHandler {
  private final AnonymousFilterSecurityConfigurationProperties
      filterSecurityConfigurationProperties;

  /**
   * Handles successful authentication by building a redirect URI with authorization code.
   *
   * @param request the HttpServletRequest.
   * @param response the HttpServletResponse.
   * @param authentication the Authentication object.
   * @throws IOException if an input or output error occurs.
   * @throws ServletException if the request could not be handled.
   */
  @Override
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
   * Handles successful authorization for OAuth2.
   *
   * @param request the HttpServletRequest.
   * @param response the HttpServletResponse.
   * @param token the OAuth2AuthorizationCodeRequestAuthenticationToken.
   * @throws IOException if an input or output error occurs.
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
