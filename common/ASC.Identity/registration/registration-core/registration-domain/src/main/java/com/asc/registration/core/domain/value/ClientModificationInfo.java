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

package com.asc.registration.core.domain.value;

import java.time.ZonedDateTime;

/**
 * ClientModificationInfo is a value object that holds information about the modification of a
 * client. It contains the timestamp when the client was modified and the identifier of the
 * modifier.
 */
public class ClientModificationInfo {
  private final ZonedDateTime modifiedOn;
  private final String modifiedBy;

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
  public String getModifiedBy() {
    return this.modifiedBy;
  }

  /** Builder class for constructing instances of {@link ClientModificationInfo}. */
  public static final class Builder {
    private ZonedDateTime modifiedOn;
    private String modifiedBy;

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
    public Builder modifiedBy(String val) {
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
