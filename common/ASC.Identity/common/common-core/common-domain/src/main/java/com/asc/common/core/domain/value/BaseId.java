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

package com.asc.common.core.domain.value;

import java.util.Objects;

/**
 * This abstract class represents a base ID for a value.
 *
 * @param <T> the type of the ID
 */
public abstract class BaseId<T> {

  /** The value of the ID. */
  private final T value;

  /**
   * Constructs a new BaseId with the specified value.
   *
   * @param value the value of the ID
   */
  protected BaseId(T value) {
    this.value = value;
  }

  /**
   * Returns the value of the ID.
   *
   * @return the value of the ID
   */
  public T getValue() {
    return value;
  }

  /**
   * Returns true if the specified object is equal to this BaseId, false otherwise.
   *
   * @param o the object to compare to this BaseId
   * @return true if the specified object is equal to this BaseId, false otherwise
   */
  public boolean equals(Object o) {
    if (this == o) return true;
    if (o == null || getClass() != o.getClass()) return false;
    BaseId<?> baseId = (BaseId<?>) o;
    return Objects.equals(value, baseId.value);
  }

  /**
   * Returns the hash code for this BaseId.
   *
   * @return the hash code for this BaseId
   */
  public int hashCode() {
    return Objects.hash(value);
  }
}
