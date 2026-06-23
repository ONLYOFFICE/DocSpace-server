// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.authorization.application.security;

import com.asc.authorization.application.configuration.properties.SecurityConfigurationProperties;
import com.asc.common.utilities.HttpUtils;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.web.util.UriComponentsBuilder;

/**
 * Utility class for handling security-related tasks such as setting security headers and managing
 * authentication error redirection.
 *
 * <p>This class provides methods to enhance HTTP responses with security protections and handle
 * redirection in case of authentication errors.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class SecurityUtils {
  private final SecurityConfigurationProperties securityConfigProperties;
  private final HttpUtils httpUtils;

  /**
   * Sets security headers on the HTTP response to mitigate common web vulnerabilities.
   *
   * <p>The headers include protections against MIME sniffing, cross-site scripting (XSS),
   * clickjacking, and other common attacks. They also enforce strict transport security and content
   * security policies.
   *
   * @param response the {@link HttpServletResponse} to set the headers on.
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
   * Redirects the response to a login page with an authentication error message.
   *
   * <p>This method constructs a redirect URI based on the request scheme and client IP. It appends
   * query parameters for the client ID, error type, and authentication context. The constructed URI
   * is logged and sent to the client as a redirect response.
   *
   * @param request the {@link HttpServletRequest} containing the original request details.
   * @param response the {@link HttpServletResponse} to send the redirect.
   * @param clientId the client ID associated with the request.
   * @param error the error code describing the authentication failure.
   * @throws IOException if an I/O error occurs during the redirect process.
   */
  public void redirectWithError(
      HttpServletRequest request, HttpServletResponse response, String clientId, String error)
      throws IOException {
    var errorRedirectUri =
        UriComponentsBuilder.fromUriString(
                String.format(
                    "%s://%s", request.getScheme(), httpUtils.getFirstForwardedHost(request)))
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
