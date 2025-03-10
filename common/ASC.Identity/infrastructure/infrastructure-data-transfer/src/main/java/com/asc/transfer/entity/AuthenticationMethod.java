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
package com.asc.transfer.entity;

/**
 * Represents the authentication methods available for client authentication.
 *
 * <p>This enum defines the supported authentication methods, each associated with a specific string
 * value. The string value can be used in configuration or API interactions to specify the desired
 * authentication mechanism.
 */
public enum AuthenticationMethod {

  /** The default authentication method using "client_secret_post". */
  DEFAULT_AUTHENTICATION("client_secret_post"),

  /** The PKCE authentication method using "none". */
  PKCE_AUTHENTICATION("none");

  private final String method;

  /**
   * Constructs an {@code AuthenticationMethod} with the specified string representation.
   *
   * @param method the string value representing the authentication method.
   */
  AuthenticationMethod(String method) {
    this.method = method;
  }

  /**
   * Retrieves the string representation of this authentication method.
   *
   * @return the authentication method as a string.
   */
  public String getMethod() {
    return method;
  }

  /**
   * Returns the {@code AuthenticationMethod} corresponding to the specified string value.
   *
   * @param method the string representation of the authentication method to look up.
   * @return the {@code AuthenticationMethod} associated with the given string.
   * @throws IllegalArgumentException if no authentication method matches the specified string.
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
