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
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.webmvc.autoconfigure.error.AbstractErrorController;
import org.springframework.boot.webmvc.error.ErrorAttributes;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;

/**
 * Controller for handling fallback errors and redirecting to the login page.
 *
 * <p>This controller processes requests to the default error path and ensures users are redirected
 * to the OAuth2 login page with the appropriate query parameters.
 */
@Controller
public class FallbackController extends AbstractErrorController {
  /** The default path for handling errors. */
  private static final String PATH = "/error";

  private HttpUtils httpUtils;

  /**
   * Constructs a new FallbackController with the provided {@link ErrorAttributes}.
   *
   * @param errorAttributes the attributes used to retrieve error information.
   */
  public FallbackController(ErrorAttributes errorAttributes) {
    super(errorAttributes);
  }

  /**
   * Sets the {@link HttpUtils} utility used for retrieving client information.
   *
   * @param httpUtils the {@link HttpUtils} instance to set.
   */
  @Autowired
  public void setHttpUtils(HttpUtils httpUtils) {
    this.httpUtils = httpUtils;
  }

  /**
   * Handles errors by redirecting the user to the login page.
   *
   * <p>The redirect URL includes query parameters for the OAuth2 type and the client ID. If no
   * client ID is provided, a default value of "error" is used.
   *
   * @param clientId the client ID associated with the error, passed as a query parameter.
   * @param request the {@link HttpServletRequest} that triggered the error.
   * @param response the {@link HttpServletResponse} used to send the redirect.
   * @throws IOException if an input or output exception occurs during the redirect.
   */
  @RequestMapping(PATH)
  public void handleError(
      @RequestParam(name = "client_id", defaultValue = "error") String clientId,
      HttpServletRequest request,
      HttpServletResponse response)
      throws IOException {
    response.sendRedirect(
        String.format(
            "%s://%s/login?type=oauth2&client_id=%s",
            request.getScheme(), httpUtils.getFirstForwardedHost(request), clientId));
  }

  /**
   * Returns the error path used by this controller.
   *
   * @return the error path as a {@link String}.
   */
  public String getErrorPath() {
    return PATH;
  }
}
