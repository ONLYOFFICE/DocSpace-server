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

package com.asc.authorization.application.controller;

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
  private final HttpUtils httpUtils;

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
                      "%s://%s", request.getScheme(), httpUtils.getFirstForwardedHost(request)))
              .path("login")
              .queryParam("client_id", clientId)
              .queryParam("type", "oauth2")
              .build());
    } finally {
      MDC.clear();
    }
  }
}
