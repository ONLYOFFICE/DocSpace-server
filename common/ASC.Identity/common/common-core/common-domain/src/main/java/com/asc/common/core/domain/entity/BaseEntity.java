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

package com.asc.common.core.domain.entity;

import java.util.Objects;

/**
 * Base class for all entities in the system.
 *
 * @param <T> The type of the entity's identifier.
 */
public abstract class BaseEntity<T> {

  /** The identifier of the entity. */
  private T id;

  /**
   * Returns the identifier of the entity.
   *
   * @return The identifier of the entity.
   */
  public T getId() {
    return id;
  }

  /**
   * Sets the identifier of the entity.
   *
   * @param id The identifier of the entity.
   */
  public void setId(T id) {
    this.id = id;
  }

  /**
   * Returns a hash code value for the object.
   *
   * @return A hash code value for the object.
   */
  @Override
  public int hashCode() {
    return Objects.hash(id);
  }

  /**
   * Indicates whether some other object is "equal to" this one.
   *
   * @param obj The reference object with which to compare.
   * @return {@code true} if this object is the same as the obj argument; {@code false} otherwise.
   */
  public boolean equals(Object obj) {
    if (this == obj) return true;
    if (obj == null || getClass() != obj.getClass()) return false;
    BaseEntity<?> that = (BaseEntity<?>) obj;
    return Objects.equals(id, that.id);
  }
}
