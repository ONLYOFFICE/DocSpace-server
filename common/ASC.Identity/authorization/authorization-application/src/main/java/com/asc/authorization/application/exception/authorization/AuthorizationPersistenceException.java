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

import static com.asc.authorization.application.security.oauth.error.AuthorizationError.ASC_IDENTITY_PERSISTENCE_ERROR;

import org.springframework.security.oauth2.core.OAuth2Error;

/** Exception thrown when there is an issue with persisting identity authorization. */
public class AuthorizationPersistenceException extends AuthorizationProcessingException {
  private static final OAuth2Error persistenceError =
      new OAuth2Error(ASC_IDENTITY_PERSISTENCE_ERROR.getCode());

  /**
   * Constructs a new AuthorizationPersistenceException with the specified detail message.
   *
   * @param message the detail message
   */
  public AuthorizationPersistenceException(String message) {
    super(persistenceError, message);
  }

  /**
   * Constructs a new AuthorizationPersistenceException with the specified cause.
   *
   * @param cause the cause of the exception
   */
  public AuthorizationPersistenceException(Throwable cause) {
    super(persistenceError, cause);
  }

  /**
   * Constructs a new AuthorizationPersistenceException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public AuthorizationPersistenceException(String message, Throwable cause) {
    super(persistenceError, message, cause);
  }
}
