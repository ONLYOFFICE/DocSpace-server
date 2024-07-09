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
