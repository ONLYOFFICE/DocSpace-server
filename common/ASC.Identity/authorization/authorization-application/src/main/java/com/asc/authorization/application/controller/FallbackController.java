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
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.autoconfigure.web.servlet.error.AbstractErrorController;
import org.springframework.boot.web.servlet.error.ErrorAttributes;
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
            request.getScheme(), httpUtils.getFirstRequestIP(request), clientId));
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
