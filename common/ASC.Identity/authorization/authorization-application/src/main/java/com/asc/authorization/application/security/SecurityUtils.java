package com.asc.authorization.application.security;

import com.asc.authorization.application.configuration.security.AnonymousFilterSecurityConfigurationProperties;
import com.asc.common.utilities.HttpUtils;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.Arrays;
import java.util.Optional;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.web.util.UriComponentsBuilder;

@Slf4j
@Component
@RequiredArgsConstructor
public class SecurityUtils {
  private final AnonymousFilterSecurityConfigurationProperties securityConfigProperties;

  /**
   * Retrieves the value of the authentication cookie.
   *
   * @param request the HttpServletRequest.
   * @return an Optional containing the value of the authentication cookie, if present.
   */
  public Optional<String> getAuthCookieValue(HttpServletRequest request) {
    return Optional.ofNullable(request.getCookies())
        .flatMap(
            cookies ->
                Arrays.stream(cookies)
                    .filter(
                        cookie ->
                            securityConfigProperties
                                .getAuthCookieName()
                                .equalsIgnoreCase(cookie.getName()))
                    .findFirst()
                    .map(Cookie::getValue));
  }

  /**
   * Sets security headers to protect against various attacks.
   *
   * @param response the HttpServletResponse to set the headers on.
   */
  public void setSecurityHeaders(HttpServletResponse response) {
    response.setHeader("X-Content-Type-Options", "nosniff");
    response.setHeader("X-Frame-Options", "DENY");
    response.setHeader("X-XSS-Protection", "1; mode=block");
    response.setHeader("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    response.setHeader("Content-Security-Policy", "default-src 'self'");
    response.setHeader("Referrer-Policy", "no-referrer");
  }

  /**
   * Redirects the response with the specified authentication error.
   *
   * @param request the HttpServletRequest.
   * @param response the HttpServletResponse.
   * @param clientId the client ID.
   * @param error the AuthenticationError.
   * @throws IOException if an I/O error occurs during the redirect process.
   */
  public void redirectWithError(
      HttpServletRequest request, HttpServletResponse response, String clientId, String error)
      throws IOException {
    var errorRedirectUri =
        UriComponentsBuilder.fromUriString(
                String.format("%s://%s", request.getScheme(), HttpUtils.getFirstRequestIP(request)))
            .path("/login")
            .queryParam("type", "oauth2")
            .queryParam("client_id", clientId)
            .queryParam("error", error)
            .build()
            .toUriString();
    log.debug("Redirecting {} to {} due to an error", clientId, errorRedirectUri);
    response.setHeader(securityConfigProperties.getRedirectHeader(), errorRedirectUri);
    response.sendRedirect(errorRedirectUri);
  }
}
