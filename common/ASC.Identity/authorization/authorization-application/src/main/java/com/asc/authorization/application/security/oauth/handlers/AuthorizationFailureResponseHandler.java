package com.asc.authorization.application.security.oauth.handlers;

import com.asc.authorization.application.configuration.security.AnonymousFilterSecurityConfigurationProperties;
import com.asc.authorization.application.exception.authorization.AuthorizationProcessingException;
import com.asc.authorization.application.security.SecurityUtils;
import com.asc.authorization.application.security.oauth.errors.AuthenticationError;
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

/** Handles authentication failures for OAuth2 authorization requests. */
@Slf4j
@Component
@RequiredArgsConstructor
public class AuthorizationFailureResponseHandler implements AuthenticationFailureHandler {
  private final AnonymousFilterSecurityConfigurationProperties
      filterSecurityConfigurationProperties;
  private final SecurityUtils securityUtils;

  /**
   * Handles authentication failure by building a redirect URI with error parameters.
   *
   * @param request the HttpServletRequest.
   * @param response the HttpServletResponse.
   * @param exception the AuthenticationException.
   * @throws IOException if an input or output error occurs.
   * @throws ServletException if the request could not be handled.
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

    if (StringUtils.hasText(error.getDescription()))
      uriBuilder.queryParam(
          OAuth2ParameterNames.ERROR_DESCRIPTION,
          UriUtils.encode(error.getDescription(), StandardCharsets.UTF_8));

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
    response.sendRedirect(redirectUri);
  }
}
