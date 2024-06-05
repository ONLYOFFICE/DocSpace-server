package com.asc.registration.core.domain.value;

import java.time.ZonedDateTime;

/**
 * ClientCreationInfo is a value object that holds information about the creation of a client. It
 * contains the timestamp when the client was created and the identifier of the creator.
 */
public class ClientCreationInfo {
  private final ZonedDateTime createdOn;
  private final String createdBy;

  private ClientCreationInfo(Builder builder) {
    createdOn = builder.createdOn;
    createdBy = builder.createdBy;
  }

  /**
   * Returns the timestamp when the client was created.
   *
   * @return the creation timestamp
   */
  public ZonedDateTime getCreatedOn() {
    return createdOn;
  }

  /**
   * Returns the identifier of the creator of the client.
   *
   * @return the creator identifier
   */
  public String getCreatedBy() {
    return createdBy;
  }

  /** Builder class for constructing instances of {@link ClientCreationInfo}. */
  public static final class Builder {
    private ZonedDateTime createdOn;
    private String createdBy;

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
      createdOn = val;
      return this;
    }

    /**
     * Sets the identifier of the creator.
     *
     * @param val the creator identifier
     * @return the Builder instance
     */
    public Builder createdBy(String val) {
      createdBy = val;
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
