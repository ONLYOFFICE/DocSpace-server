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

package com.asc.authorization.application.exception.authorization;

import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2Error;

/** Exception thrown when there is an issue with processing authorization. */
public class AuthorizationProcessingException extends OAuth2AuthenticationException {

  /**
   * Constructs a new AuthorizationProcessingException with the specified OAuth2Error and detail
   * message.
   *
   * @param error the OAuth2Error describing the exception
   * @param message the detail message
   */
  public AuthorizationProcessingException(OAuth2Error error, String message) {
    super(error, message);
  }

  /**
   * Constructs a new AuthorizationProcessingException with the specified OAuth2Error and cause.
   *
   * @param error the OAuth2Error describing the exception
   * @param cause the cause of the exception
   */
  public AuthorizationProcessingException(OAuth2Error error, Throwable cause) {
    super(error, cause);
  }

  /**
   * Constructs a new AuthorizationProcessingException with the specified OAuth2Error, detail
   * message, and cause.
   *
   * @param error the OAuth2Error describing the exception
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public AuthorizationProcessingException(OAuth2Error error, String message, Throwable cause) {
    super(error, message, cause);
  }
}
