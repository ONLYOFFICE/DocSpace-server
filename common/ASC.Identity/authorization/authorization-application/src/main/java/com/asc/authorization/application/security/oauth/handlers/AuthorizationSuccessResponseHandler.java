package com.asc.authorization.application.security.oauth.handlers;

import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationCodeRequestAuthenticationToken;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.stereotype.Component;

/** Handles successful authentication for OAuth2 authorization requests. */
@Slf4j
@Component
public class AuthorizationSuccessResponseHandler implements AuthenticationSuccessHandler {
  private static final String DISABLE_REDIRECT_HEADER = "X-Disable-Redirect";
  private static final String REDIRECT_HEADER = "X-Redirect-URI";

  /**
   * Handles successful authentication by building a redirect URI with authorization code.
   *
   * @param request the HttpServletRequest.
   * @param response the HttpServletResponse.
   * @param authentication the Authentication object.
   * @throws IOException if an input or output error occurs.
   * @throws ServletException if the request could not be handled.
   */
  public void onAuthenticationSuccess(
      HttpServletRequest request, HttpServletResponse response, Authentication authentication)
      throws IOException, ServletException {
    log.debug("Authorization success");

    if (authentication instanceof OAuth2AuthorizationCodeRequestAuthenticationToken token) {
      log.debug("Handling successful authorization for client: {}", token.getClientId());

      var state = token.getState();
      var redirectUri = token.getRedirectUri();
      var authorizationCode = token.getAuthorizationCode().getTokenValue();

      var redirectUrl =
          new StringBuilder(String.format("%s?code=%s", redirectUri, authorizationCode));
      if (state != null && !state.isBlank()) {
        redirectUrl.append(String.format("&state=%s", state));
      }

      var disableRedirectHeader = request.getHeader(DISABLE_REDIRECT_HEADER);
      if (disableRedirectHeader != null) {
        log.debug("Disabling redirect, setting header {} with redirect URL", REDIRECT_HEADER);
        response.setStatus(HttpStatus.OK.value());
        response.setHeader(REDIRECT_HEADER, redirectUrl.toString());
      } else {
        log.debug("Redirecting to URL: {}", redirectUrl);
        response.sendRedirect(redirectUrl.toString());
      }
    } else {
      log.warn(
          "Authentication object is not of type OAuth2AuthorizationCodeRequestAuthenticationToken");
      response.sendError(
          HttpStatus.INTERNAL_SERVER_ERROR.value(), "Authentication type is not supported");
    }
  }
}
