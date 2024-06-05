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
