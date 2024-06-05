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
