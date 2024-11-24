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

package com.asc.registration.application.security.filter;

import com.asc.common.application.client.AscApiClient;
import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.application.transfer.response.AscSettingsResponse;
import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.common.utilities.HttpUtils;
import com.asc.registration.application.security.authentication.AscAuthenticationTokenPrincipal;
import jakarta.servlet.http.HttpServletRequest;
import java.net.URI;
import java.util.concurrent.atomic.AtomicReference;
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
  private final AscApiClient apiClient;
  private final HttpUtils httpUtils;

  /**
   * Process ASC cookies and authenticate the request.
   *
   * @param request the HTTP request
   * @param ascCookieName the name of the ASC cookie
   * @param ascCookieValue the value of the ASC cookie
   * @return the principal containing user, tenant, and settings information
   * @throws BadCredentialsException if authentication fails
   */
  public AscAuthenticationTokenPrincipal processAscCookies(
      HttpServletRequest request, String ascCookieName, String ascCookieValue)
      throws BadCredentialsException {
    var cookie = String.format("%s=%s", ascCookieName, ascCookieValue);

    var address =
        httpUtils
            .getRequestHostAddress(request)
            .orElseThrow(() -> new BadCredentialsException("Could not find ASC address"));

    try {
      var uri = URI.create(address);
      AtomicReference<AscPersonResponse> me = new AtomicReference<>();
      AtomicReference<AscTenantResponse> tenant = new AtomicReference<>();
      AtomicReference<AscSettingsResponse> settings = new AtomicReference<>();

      var userThread =
          Thread.ofVirtual().start(() -> me.set(apiClient.getMe(uri, cookie).getResponse()));
      var tenantThread =
          Thread.ofVirtual()
              .start(() -> tenant.set(apiClient.getTenant(uri, cookie).getResponse()));
      var settingsThread =
          Thread.ofVirtual()
              .start(() -> settings.set(apiClient.getSettings(uri, cookie).getResponse()));

      userThread.join();
      tenantThread.join();
      settingsThread.join();

      return new AscAuthenticationTokenPrincipal(me.get(), tenant.get(), settings.get());
    } catch (InterruptedException e) {
      Thread.currentThread().interrupt();
      throw new BadCredentialsException("Something went wrong while fetching data", e);
    }
  }
}
