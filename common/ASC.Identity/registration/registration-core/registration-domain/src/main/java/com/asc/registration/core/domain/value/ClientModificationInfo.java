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

package com.asc.registration.core.domain.value;

import com.asc.common.core.domain.value.UserId;
import java.time.ZonedDateTime;

/**
 * ClientModificationInfo is a value object that holds information about the modification of a
 * client. It contains the timestamp when the client was modified and the identifier of the
 * modifier.
 */
public class ClientModificationInfo {
  private final ZonedDateTime modifiedOn;
  private final UserId modifiedBy;

  private ClientModificationInfo(Builder builder) {
    this.modifiedOn = builder.modifiedOn;
    this.modifiedBy = builder.modifiedBy;
  }

  /**
   * Returns the timestamp when the client was modified.
   *
   * @return the modification timestamp
   */
  public ZonedDateTime getModifiedOn() {
    return this.modifiedOn;
  }

  /**
   * Returns the identifier of the modifier of the client.
   *
   * @return the modifier identifier
   */
  public UserId getModifiedBy() {
    return this.modifiedBy;
  }

  /** Builder class for constructing instances of {@link ClientModificationInfo}. */
  public static final class Builder {
    private ZonedDateTime modifiedOn;
    private UserId modifiedBy;

    private Builder() {}

    /**
     * Creates a new Builder instance.
     *
     * @return a new Builder
     */
    public static Builder builder() {
      return new Builder();
    }

    /**
     * Sets the modification timestamp.
     *
     * @param val the modification timestamp
     * @return the Builder instance
     */
    public Builder modifiedOn(ZonedDateTime val) {
      this.modifiedOn = val;
      return this;
    }

    /**
     * Sets the identifier of the modifier.
     *
     * @param val the modifier identifier
     * @return the Builder instance
     */
    public Builder modifiedBy(UserId val) {
      this.modifiedBy = val;
      return this;
    }

    /**
     * Builds and returns a new {@link ClientModificationInfo} instance.
     *
     * @return a new ClientModificationInfo instance
     */
    public ClientModificationInfo build() {
      return new ClientModificationInfo(this);
    }
  }
}
