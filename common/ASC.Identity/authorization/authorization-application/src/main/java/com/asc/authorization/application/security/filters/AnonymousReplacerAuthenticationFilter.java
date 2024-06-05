package com.asc.authorization.application.security.filters;

import com.asc.authorization.application.configuration.security.AnonymousFilterSecurityConfigurationProperties;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.Arrays;
import java.util.Optional;
import java.util.regex.Pattern;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpMethod;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/** Filter for replacing anonymous authentication with actual authentication based on a cookie. */
@Slf4j
@Component
@RequiredArgsConstructor
public class AnonymousReplacerAuthenticationFilter extends OncePerRequestFilter {
  private final AuthenticationManager authenticationManager;
  private final AnonymousFilterSecurityConfigurationProperties securityConfigProperties;

  private static final Pattern AUTHORIZE_PATTERN = Pattern.compile("/oauth2/authorize");
  private static final Pattern LOGIN_PATTERN = Pattern.compile("/oauth2/login");

  /**
   * Filters requests to replace anonymous authentication with actual authentication.
   *
   * @param request the HttpServletRequest.
   * @param response the HttpServletResponse.
   * @param chain the FilterChain.
   * @throws ServletException if an error occurs during the filter process.
   * @throws IOException if an I/O error occurs during the filter process.
   */
  protected void doFilterInternal(
      HttpServletRequest request, HttpServletResponse response, FilterChain chain)
      throws ServletException, IOException {
    var clientId = request.getParameter(securityConfigProperties.getClientIdParam());

    if (clientId == null || clientId.isBlank()) {
      log.warn(
          "Missing or empty '{}' in query string", securityConfigProperties.getClientIdParam());
      response.sendError(HttpServletResponse.SC_BAD_REQUEST, "Missing or empty 'client_id'");
      return;
    }

    var authCookie =
        Optional.ofNullable(request.getCookies())
            .flatMap(
                cookies ->
                    Arrays.stream(cookies)
                        .filter(
                            cookie ->
                                securityConfigProperties
                                    .getAuthCookieName()
                                    .equalsIgnoreCase(cookie.getName()))
                        .findFirst());

    if (authCookie.isEmpty()) {
      log.warn("Missing '{}' cookie", securityConfigProperties.getAuthCookieName());
      response.sendError(HttpServletResponse.SC_UNAUTHORIZED, "Missing 'asc_auth_key' cookie");
      return;
    }

    var authCookieValue = authCookie.get().getValue();
    var authenticationToken = new UsernamePasswordAuthenticationToken(clientId, authCookieValue);

    try {
      var authentication = authenticationManager.authenticate(authenticationToken);
      if (authentication.isAuthenticated()) {
        SecurityContextHolder.getContext().setAuthentication(authentication);
      }
    } catch (Exception ex) {
      log.error("Authentication failed", ex);
      response.sendError(HttpServletResponse.SC_UNAUTHORIZED, "Authentication failed");
      return;
    }

    setSecurityHeaders(response);
    chain.doFilter(request, response);
  }

  /**
   * Determines whether the filter should be applied to the given request.
   *
   * @param request the HttpServletRequest.
   * @return true if the filter should not be applied, false otherwise.
   */
  protected boolean shouldNotFilter(HttpServletRequest request) {
    var path = request.getRequestURI();
    return !(AUTHORIZE_PATTERN.matcher(path).find()
        || (LOGIN_PATTERN.matcher(path).find() && HttpMethod.POST.matches(request.getMethod())));
  }

  /**
   * Sets security headers to protect against various attacks.
   *
   * @param response the HttpServletResponse to set the headers on.
   */
  private void setSecurityHeaders(HttpServletResponse response) {
    response.setHeader("X-Content-Type-Options", "nosniff");
    response.setHeader("X-Frame-Options", "DENY");
    response.setHeader("X-XSS-Protection", "1; mode=block");
    response.setHeader("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    response.setHeader("Content-Security-Policy", "default-src 'self'");
    response.setHeader("Referrer-Policy", "no-referrer");
  }
}
