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

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.RestController;

/**
 * Controller for handling requests to the OAuth 2.0 Authorization Server metadata endpoint.
 *
 * <p>This controller responds to OPTIONS requests for the "/.well-known/oauth-authorization-server"
 * endpoint, typically used for CORS preflight or discovery purposes.
 */
@RestController
public class WellKnownController {
  /**
   * Handles HTTP OPTIONS requests for the OAuth 2.0 Authorization Server metadata endpoint.
   *
   * <p>This method responds with HTTP 200 OK and no body, allowing CORS preflight or discovery
   * requests to succeed without additional processing.
   *
   * @return a {@link ResponseEntity} with HTTP 200 OK and no content.
   */
  @RequestMapping(value = "/.well-known/oauth-authorization-server", method = RequestMethod.OPTIONS)
  public ResponseEntity<?> handleOptions() {
    return ResponseEntity.ok().build();
  }
}
