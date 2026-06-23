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
