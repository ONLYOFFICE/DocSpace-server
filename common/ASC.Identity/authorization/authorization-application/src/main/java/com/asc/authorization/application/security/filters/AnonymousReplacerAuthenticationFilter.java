package com.asc.authorization.application.security.filters;

import com.asc.authorization.application.configuration.security.AnonymousFilterSecurityConfigurationProperties;
import com.asc.authorization.application.exception.authentication.AuthenticationProcessingException;
import com.asc.authorization.application.exception.client.RegisteredClientPermissionException;
import com.asc.authorization.application.security.SecurityUtils;
import com.asc.authorization.application.security.oauth.errors.AuthenticationError;
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

/** Filter for replacing anonymous authentication with actual authentication based on a cookie. */
@Slf4j
@Component
@RequiredArgsConstructor
public class AnonymousReplacerAuthenticationFilter extends OncePerRequestFilter {
  private static final Pattern AUTHORIZE_PATTERN = Pattern.compile("/oauth2/authorize");
  private static final Pattern LOGIN_PATTERN = Pattern.compile("/oauth2/login");

  private final AuthenticationManager authenticationManager;
  private final SecurityUtils securityUtils;
  private final AnonymousFilterSecurityConfigurationProperties securityConfigProperties;

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
      securityUtils.redirectWithError(
          request, response, clientId, AuthenticationError.MISSING_CLIENT_ID_ERROR.getCode());
      return;
    }

    var authCookieValue = securityUtils.getAuthCookieValue(request);
    if (authCookieValue.isEmpty()) {
      log.warn("Missing '{}' cookie", securityConfigProperties.getAuthCookieName());
      securityUtils.redirectWithError(
          request, response, clientId, AuthenticationError.MISSING_ASC_COOKIE_ERROR.getCode());
      return;
    }

    try {
      var authenticationToken =
          new UsernamePasswordAuthenticationToken(clientId, authCookieValue.get());
      var authentication = authenticationManager.authenticate(authenticationToken);
      if (authentication.isAuthenticated()) {
        SecurityContextHolder.getContext().setAuthentication(authentication);
        securityUtils.setSecurityHeaders(response);
        chain.doFilter(request, response);
      }
    } catch (RegisteredClientPermissionException pex) {
      securityUtils.redirectWithError(
          request,
          response,
          clientId,
          AuthenticationError.CLIENT_PERMISSION_DENIED_ERROR.getCode());
    } catch (AuthenticationProcessingException aex) {
      securityUtils.redirectWithError(request, response, clientId, aex.getError().getCode());
    } catch (Exception e) {
      securityUtils.redirectWithError(
          request, response, clientId, AuthenticationError.SOMETHING_WENT_WRONG_ERROR.getCode());
    }
  }

  /**
   * Determines whether the filter should be applied to the given request.
   *
   * @param request the HttpServletRequest.
   * @return true if the filter should not be applied, false otherwise.
   */
  protected boolean shouldNotFilter(HttpServletRequest request) {
    var path = request.getRequestURI();
    return !(LOGIN_PATTERN.matcher(path).find() && HttpMethod.POST.matches(request.getMethod()))
        && !(AUTHORIZE_PATTERN.matcher(path).find());
  }
}
