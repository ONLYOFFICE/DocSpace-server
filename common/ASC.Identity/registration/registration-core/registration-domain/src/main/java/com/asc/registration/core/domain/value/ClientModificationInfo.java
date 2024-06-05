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
    modifiedOn = builder.modifiedOn;
    modifiedBy = builder.modifiedBy;
  }

  /**
   * Returns the timestamp when the client was modified.
   *
   * @return the modification timestamp
   */
  public ZonedDateTime getModifiedOn() {
    return modifiedOn;
  }

  /**
   * Returns the identifier of the modifier of the client.
   *
   * @return the modifier identifier
   */
  public String getModifiedBy() {
    return modifiedBy;
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
      modifiedOn = val;
      return this;
    }

    /**
     * Sets the identifier of the modifier.
     *
     * @param val the modifier identifier
     * @return the Builder instance
     */
    public Builder modifiedBy(String val) {
      modifiedBy = val;
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
