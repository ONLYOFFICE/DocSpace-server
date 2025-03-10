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

package com.asc.common.core.domain.value.enums;

import java.util.stream.Stream;

/** Represents an enumeration of audit codes used in the domain layer. */
public enum AuditCode {

  /** Audit code for creating a client. */
  CREATE_CLIENT(9901),

  /** Audit code for updating a client. */
  UPDATE_CLIENT(9902),

  /** Audit code for regenerating a secret. */
  REGENERATE_SECRET(9903),

  /** Audit code for deleting a client. */
  DELETE_CLIENT(9904),

  /** Audit code for changing a client's activation status. */
  CHANGE_CLIENT_ACTIVATION(9905),

  /** Audit code for changing a client's activation status. */
  CHANGE_CLIENT_VISIBILITY(9906),

  /** Audit code for revoking a user's client access. */
  REVOKE_USER_CLIENT(9907),

  /** Audit code for generating a user's authorization_code authorization. */
  GENERATE_AUTHORIZATION_CODE_TOKEN(9908),

  /** Audit code for generating a user's personal_access_token authorization. */
  GENERATE_PERSONAL_ACCESS_TOKEN(9909);

  private final int code;

  AuditCode(int code) {
    this.code = code;
  }

  /**
   * Returns the code associated with this audit code.
   *
   * @return The code associated with this audit code.
   */
  public int getCode() {
    return code;
  }

  /**
   * Returns the audit code associated with the given code.
   *
   * @param code The code to search for.
   * @return The audit code associated with the given code.
   * @throws IllegalArgumentException If no audit code is found with the given code.
   */
  public static AuditCode of(int code) {
    return Stream.of(AuditCode.values())
        .filter(p -> p.code == code)
        .findFirst()
        .orElseThrow(IllegalArgumentException::new);
  }
}
