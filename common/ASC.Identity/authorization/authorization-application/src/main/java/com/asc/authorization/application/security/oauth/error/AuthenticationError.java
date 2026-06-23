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

package com.asc.authorization.application.security.oauth.error;

import lombok.Getter;

/** Enum representing various authentication errors. */
@Getter
public enum AuthenticationError {
  /** Error indicating that the authentication method is not supported. */
  AUTHENTICATION_NOT_SUPPORTED_ERROR("authentication_not_supported_error"),

  /** Error indicating that the client is disabled. */
  CLIENT_DISABLED_ERROR("client_disabled_error"),

  /** Error indicating that the client was not found. */
  CLIENT_NOT_FOUND_ERROR("client_not_found_error"),

  /** Error indicating that the client does not have permission. */
  CLIENT_PERMISSION_DENIED_ERROR("client_permission_denied_error"),

  MISSING_ASC_SIGNATURE("missing_asc_signature_error"),

  /** Error indicating that the client ID is missing. */
  MISSING_CLIENT_ID_ERROR("missing_client_id_error"),

  /** Error indicating that something went wrong during the authentication process. */
  SOMETHING_WENT_WRONG_ERROR("something_went_wrong_error");

  /** The error code associated with the authentication error. */
  private final String code;

  /**
   * Constructs an AuthenticationError with the specified error code.
   *
   * @param code the error code
   */
  AuthenticationError(String code) {
    this.code = code;
  }
}
