// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.common.core.domain.value;

import java.util.Objects;

/** This class represents a consent ID. */
public class ConsentId {

  /** The registered client ID. */
  private final String registeredClientId;

  /** The principal name. */
  private final String principalName;

  /**
   * Constructs a new ConsentId with the specified registered client ID and principal name.
   *
   * @param registeredClientId the registered client ID
   * @param principalName the principal name
   */
  public ConsentId(String registeredClientId, String principalName) {
    this.registeredClientId = registeredClientId;
    this.principalName = principalName;
  }

  /**
   * Returns the registered client ID.
   *
   * @return the registered client ID
   */
  public String getRegisteredClientId() {
    return registeredClientId;
  }

  /**
   * Returns the principal name.
   *
   * @return the principal name
   */
  public String getPrincipalName() {
    return principalName;
  }

  /**
   * Returns true if the specified object is equal to this ConsentId, false otherwise.
   *
   * @param o the object to compare to this ConsentId
   * @return true if the specified object is equal to this ConsentId, false otherwise
   */
  public boolean equals(Object o) {
    if (this == o) return true;
    if (o == null || getClass() != o.getClass()) return false;
    var that = (ConsentId) o;
    return registeredClientId.equals(that.registeredClientId)
        && principalName.equals(that.principalName);
  }

  /**
   * Returns the hash code for this ConsentId.
   *
   * @return the hash code for this ConsentId
   */
  public int hashCode() {
    return Objects.hash(registeredClientId, principalName);
  }
}
