package com.asc.registration.application.security;

import com.asc.common.application.client.AscApiClient;
import com.asc.common.utilities.HttpUtils;
import jakarta.servlet.http.HttpServletRequest;
import java.net.URI;
import java.util.Arrays;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.stereotype.Component;

/**
 * AscCookieAuthenticationProcessor is responsible for processing ASC cookies and authenticating the
 * request based on the presence of a specific authentication cookie. It interacts with an external
 * API client to fetch user, tenant, and settings data.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AscCookieAuthenticationProcessor {
  private final String AUTH_COOKIE_NAME = "asc_auth_key";

  private final AscApiClient apiClient;

  /**
   * Process ASC cookies and authenticate the request.
   *
   * @param request the HTTP request
   * @throws BadCredentialsException if authentication fails
   */
  public void processAscCookies(HttpServletRequest request) throws BadCredentialsException {
    log.debug("Trying to authenticate an incoming request");

    var cookies = request.getCookies();
    if (cookies == null || cookies.length < 1)
      throw new BadCredentialsException("Could not find any authentication cookie");

    var authCookie =
        Arrays.stream(cookies)
            .filter(c -> c.getName().equalsIgnoreCase(AUTH_COOKIE_NAME))
            .findFirst();

    if (authCookie.isEmpty()) throw new BadCredentialsException("Could not find ASC auth cookie");

    var ascCookie = String.format("%s=%s", authCookie.get().getName(), authCookie.get().getValue());

    var address =
        HttpUtils.getRequestHostAddress(request)
            .orElseThrow(() -> new BadCredentialsException("Could not find ASC address"));

    try {
      var uri = URI.create(address);
      var userThread =
          Thread.ofVirtual()
              .start(
                  () ->
                      request.setAttribute(
                          "person", apiClient.getMe(uri, ascCookie).getResponse()));
      var tenantThread =
          Thread.ofVirtual()
              .start(
                  () ->
                      request.setAttribute(
                          "tenant", apiClient.getTenant(uri, ascCookie).getResponse()));
      var settingsThread =
          Thread.ofVirtual()
              .start(
                  () ->
                      request.setAttribute(
                          "settings", apiClient.getSettings(uri, ascCookie).getResponse()));

      userThread.join();
      tenantThread.join();
      settingsThread.join();
    } catch (InterruptedException e) {
      Thread.currentThread().interrupt();
      throw new BadCredentialsException("Something went wrong while fetching data", e);
    }
  }
}
