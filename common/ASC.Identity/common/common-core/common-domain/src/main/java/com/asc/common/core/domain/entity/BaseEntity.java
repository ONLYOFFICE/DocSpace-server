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
  @Override
  public boolean equals(Object obj) {
    if (this == obj) return true;
    if (obj == null || getClass() != obj.getClass()) return false;
    BaseEntity<?> that = (BaseEntity<?>) obj;
    return Objects.equals(id, that.id);
  }
}
