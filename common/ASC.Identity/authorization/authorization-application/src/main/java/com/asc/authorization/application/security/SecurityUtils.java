// (c) Copyright Ascensio System SIA 2009-2024
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
  private final HttpUtils httpUtils;

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
                String.format("%s://%s", request.getScheme(), httpUtils.getFirstRequestIP(request)))
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
