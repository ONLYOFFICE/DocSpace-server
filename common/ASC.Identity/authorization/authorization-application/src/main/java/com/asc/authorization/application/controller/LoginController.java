// (c) Copyright Ascensio System SIA 2009-2025
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
                      "%s://%s", request.getScheme(), httpUtils.getFirstRequestIP(request)))
              .path("login")
              .queryParam("client_id", clientId)
              .queryParam("type", "oauth2")
              .build());
    } finally {
      MDC.clear();
    }
  }
}
