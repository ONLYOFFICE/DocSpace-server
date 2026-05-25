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
 * ClientCreationInfo is a value object that holds information about the creation of a client. It
 * contains the timestamp when the client was created and the identifier of the creator.
 */
public class ClientCreationInfo {
  private final ZonedDateTime createdOn;
  private final UserId createdBy;

  private ClientCreationInfo(Builder builder) {
    this.createdOn = builder.createdOn;
    this.createdBy = builder.createdBy;
  }

  /**
   * Returns the timestamp when the client was created.
   *
   * @return the creation timestamp
   */
  public ZonedDateTime getCreatedOn() {
    return this.createdOn;
  }

  /**
   * Returns the identifier of the creator of the client.
   *
   * @return the creator identifier
   */
  public UserId getCreatedBy() {
    return this.createdBy;
  }

  /** Builder class for constructing instances of {@link ClientCreationInfo}. */
  public static final class Builder {
    private ZonedDateTime createdOn;
    private UserId createdBy;

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
     * Sets the creation timestamp.
     *
     * @param val the creation timestamp
     * @return the Builder instance
     */
    public Builder createdOn(ZonedDateTime val) {
      this.createdOn = val;
      return this;
    }

    /**
     * Sets the identifier of the creator.
     *
     * @param val the creator identifier
     * @return the Builder instance
     */
    public Builder createdBy(UserId val) {
      this.createdBy = val;
      return this;
    }

    /**
     * Builds and returns a new {@link ClientCreationInfo} instance.
     *
     * @return a new ClientCreationInfo instance
     */
    public ClientCreationInfo build() {
      return new ClientCreationInfo(this);
    }
  }
}
