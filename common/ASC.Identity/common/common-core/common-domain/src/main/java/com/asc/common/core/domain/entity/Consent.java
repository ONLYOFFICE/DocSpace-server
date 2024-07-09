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

import com.asc.common.core.domain.exception.ConsentDomainException;
import com.asc.common.core.domain.value.ConsentId;
import com.asc.common.core.domain.value.enums.ConsentStatus;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;

/**
 * Represents a consent entity with a unique identifier, set of scopes, modification date and
 * status. This class extends AggregateRoot and uses the Builder pattern for construction.
 */
public class Consent extends AggregateRoot<ConsentId> {

  /** Constant for UTC timezone. */
  private static final String UTC = "UTC";

  /** The set of scopes for this consent. */
  private final Set<String> scopes;

  /** The date and time when this consent was last modified. */
  private ZonedDateTime modifiedOn;

  /** The status of this consent. */
  private ConsentStatus status;

  /**
   * Private constructor to enforce the use of the Builder pattern.
   *
   * @param builder the Builder object containing the necessary data.
   */
  private Consent(Builder builder) {
    super.setId(builder.id);
    scopes = builder.scopes;
    modifiedOn = builder.modifiedOn;
    status = builder.status;
    validate();
  }

  /** Initializes the consent with the status set to ACTIVE and updates the modification date. */
  public void initialize() {
    status = ConsentStatus.ACTIVE;
    updateModification();
    validate();
  }

  /**
   * Invalidates the consent by setting the status to INVALIDATED and updates the modification date.
   */
  public void invalidate() {
    validateStatus();
    status = ConsentStatus.INVALIDATED;
    updateModification();
    validate();
  }

  /** Updates the modification date of the consent. */
  private void updateModification() {
    modifiedOn = ZonedDateTime.now(ZoneId.of(UTC));
  }

  /** The Builder class for constructing Consent objects. */
  public static final class Builder {

    private ConsentId id;
    private Set<String> scopes;
    private ZonedDateTime modifiedOn;
    private ConsentStatus status;

    /**
     * Sets the ID of the consent.
     *
     * @param val the ConsentId value.
     * @return the Builder object.
     */
    public Builder id(ConsentId val) {
      id = val;
      return this;
    }

    /**
     * Sets the scopes of the consent.
     *
     * @param val the Set of String values.
     * @return the Builder object.
     */
    public Builder scopes(Set<String> val) {
      scopes = val;
      return this;
    }

    /**
     * Sets the modification date of the consent.
     *
     * @param val the ZonedDateTime value.
     * @return the Builder object.
     */
    public Builder modifiedOn(ZonedDateTime val) {
      modifiedOn = val;
      return this;
    }

    /**
     * Sets the status of the consent.
     *
     * @param val the ConsentStatus value.
     * @return the Builder object.
     */
    public Builder status(ConsentStatus val) {
      status = val;
      return this;
    }

    /**
     * Builds and returns the Consent object.
     *
     * @return the Consent object.
     */
    public Consent build() {
      return new Consent(this);
    }

    /** Private constructor to enforce the use of the Builder pattern. */
    private Builder() {}

    /**
     * Returns a new Builder object.
     *
     * @return the Builder object.
     */
    public static Builder builder() {
      return new Builder();
    }
  }

  /**
   * Returns the set of scopes for this consent.
   *
   * @return the Set of String values.
   */
  public Set<String> getScopes() {
    return scopes;
  }

  /**
   * Returns the date and time when this consent was last modified.
   *
   * @return the ZonedDateTime value.
   */
  public ZonedDateTime getModifiedOn() {
    return modifiedOn;
  }

  /**
   * Returns the status of this consent.
   *
   * @return the ConsentStatus value.
   */
  public ConsentStatus getStatus() {
    return status;
  }

  /** Validates the status of the consent. */
  private void validateStatus() {
    if (status == null) throw new ConsentDomainException("Consent has not been initialized yet");
  }

  /** Validates the state of the consent. */
  private void validate() {
    if (getId() == null)
      throw new ConsentDomainException("Consent is in invalid state due to missing ID");
    var principalName = getId().getPrincipalName();
    if (principalName == null || principalName.isBlank())
      throw new ConsentDomainException("Consent is in invalid state due to missing principal name");
    var registeredClientId = getId().getRegisteredClientId();
    if (registeredClientId == null || registeredClientId.isBlank())
      throw new ConsentDomainException(
          "Consent is in invalid state due to missing registered client id");
    if (status == null)
      throw new ConsentDomainException("Consent is in invalid state due to missing status");
    if (scopes == null || scopes.isEmpty())
      throw new ConsentDomainException("Consent is in invalid state due to missing scopes");
  }
}
