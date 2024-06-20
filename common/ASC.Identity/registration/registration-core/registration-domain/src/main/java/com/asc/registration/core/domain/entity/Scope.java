package com.asc.registration.core.domain.entity;

import com.asc.common.core.domain.entity.AggregateRoot;
import com.asc.registration.core.domain.exception.ScopeDomainException;

/**
 * Represents the Scope aggregate root in the domain-driven design context. This class encapsulates
 * scope-specific information and behaviors.
 */
public class Scope extends AggregateRoot<String> {
  private String name;
  private String group;
  private String type;

  /**
   * Constructs a Scope instance using the provided builder.
   *
   * @param builder the builder instance used to create a Scope
   */
  private Scope(Builder builder) {
    super.setId(builder.name);
    this.name = builder.name;
    this.group = builder.group;
    this.type = builder.type;
    validate();
  }

  /**
   * Returns the name of the scope.
   *
   * @return the name of the scope
   */
  public String getName() {
    return this.name;
  }

  /**
   * Returns the group of the scope.
   *
   * @return the group of the scope
   */
  public String getGroup() {
    return this.group;
  }

  /**
   * Returns the type of the scope.
   *
   * @return the type of the scope
   */
  public String getType() {
    return this.type;
  }

  /**
   * Validates the scope's state to ensure all required fields are set and valid.
   *
   * @throws ScopeDomainException if any required field is null or empty
   */
  private void validate() {
    if (this.name == null || this.name.isBlank())
      throw new ScopeDomainException("Scope name must not be null or empty");
    if (this.group == null || this.group.isBlank())
      throw new ScopeDomainException("Scope group must not be null or empty");
    if (this.type == null || this.type.isBlank())
      throw new ScopeDomainException("Scope type must not be null or empty");
  }

  /**
   * Updates the group of the scope.
   *
   * @param newGroup the new group for the scope
   * @throws ScopeDomainException if the new group is null or empty
   */
  public void updateGroup(String newGroup) {
    this.group = newGroup;
    validate();
  }

  /**
   * Updates the type of the scope.
   *
   * @param newType the new type for the scope
   * @throws ScopeDomainException if the new type is null or empty
   */
  public void updateType(String newType) {
    this.type = newType;
    validate();
  }

  /** Builder class for constructing instances of {@link Scope}. */
  public static final class Builder {
    private String name;
    private String group;
    private String type;

    private Builder() {}

    /**
     * Returns a new Builder instance.
     *
     * @return a new Builder instance
     */
    public static Builder builder() {
      return new Builder();
    }

    /**
     * Sets the name of the scope.
     *
     * @param name the name of the scope
     * @return the Builder instance
     */
    public Builder name(String name) {
      this.name = name;
      return this;
    }

    /**
     * Sets the group of the scope.
     *
     * @param group the group of the scope
     * @return the Builder instance
     */
    public Builder group(String group) {
      this.group = group;
      return this;
    }

    /**
     * Sets the type of the scope.
     *
     * @param type the type of the scope
     * @return the Builder instance
     */
    public Builder type(String type) {
      this.type = type;
      return this;
    }

    /**
     * Builds and returns a new {@link Scope} instance.
     *
     * @return a new Scope instance
     */
    public Scope build() {
      return new Scope(this);
    }
  }
}
