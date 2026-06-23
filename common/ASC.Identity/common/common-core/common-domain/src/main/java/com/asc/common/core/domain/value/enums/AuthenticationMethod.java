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

package com.asc.common.core.domain.value.enums;

/** Represents an enumeration of authentication methods used in the domain layer. */
public enum AuthenticationMethod {

  /** Default authentication method using client secret post. */
  DEFAULT_AUTHENTICATION("client_secret_post"),

  /** PKCE authentication method using none. */
  PKCE_AUTHENTICATION("none");

  private final String method;

  AuthenticationMethod(String method) {
    this.method = method;
  }

  /**
   * Returns the method associated with this authentication method.
   *
   * @return The method associated with this authentication method.
   */
  public String getMethod() {
    return method;
  }

  /**
   * Returns the authentication method associated with the given method.
   *
   * @param method The method to search for.
   * @return The authentication method associated with the given method.
   * @throws IllegalArgumentException If no authentication method is found with the given method.
   */
  public static AuthenticationMethod fromMethod(String method) {
    for (AuthenticationMethod authenticationMethod : values()) {
      if (authenticationMethod.getMethod().equals(method)) {
        return authenticationMethod;
      }
    }

    throw new IllegalArgumentException("No enum constant for method: " + method);
  }
}
