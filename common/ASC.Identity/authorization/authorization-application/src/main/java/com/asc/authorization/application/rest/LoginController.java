package com.asc.authorization.application.rest;

import com.asc.common.utilities.HttpUtils;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.util.UriComponentsBuilder;

/** Controller for handling OAuth2 login requests. */
@Slf4j
@Controller
@RequiredArgsConstructor
public class LoginController {
  private static final String CLIENT_ID = "client_id";

  /**
   * Handles login requests and redirects to the login page with appropriate query parameters.
   *
   * @param request the {@link HttpServletRequest} that triggered the login request.
   * @param clientId the client ID requesting login.
   * @return a redirect URL to the login page with query parameters.
   */
  @GetMapping("/oauth2/login")
  public String login(HttpServletRequest request, @RequestParam(name = CLIENT_ID) String clientId) {
    try {
      MDC.put("client_id", clientId);
      log.info("Get login request");

      return String.format(
          "redirect:%s",
          UriComponentsBuilder.fromUriString(
                  String.format(
                      "%s://%s", request.getScheme(), HttpUtils.getFirstRequestIP(request)))
              .path("login")
              .queryParam("client_id", clientId)
              .queryParam("type", "oauth2")
              .build());
    } finally {
      MDC.clear();
    }
  }
}
