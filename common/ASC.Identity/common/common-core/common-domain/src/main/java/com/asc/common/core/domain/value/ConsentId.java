package com.asc.common.core.domain.value;

import java.util.Objects;

/** This class represents a consent ID. */
public class ConsentId {

  /** The registered client ID. */
  private final String registeredClientId;

  /** The principal name. */
  private final String principalName;

  /**
   * Constructs a new ConsentId with the specified registered client ID and principal name.
   *
   * @param registeredClientId the registered client ID
   * @param principalName the principal name
   */
  public ConsentId(String registeredClientId, String principalName) {
    this.registeredClientId = registeredClientId;
    this.principalName = principalName;
  }

  /**
   * Returns the registered client ID.
   *
   * @return the registered client ID
   */
  public String getRegisteredClientId() {
    return registeredClientId;
  }

  /**
   * Returns the principal name.
   *
   * @return the principal name
   */
  public String getPrincipalName() {
    return principalName;
  }

  /**
   * Returns true if the specified object is equal to this ConsentId, false otherwise.
   *
   * @param o the object to compare to this ConsentId
   * @return true if the specified object is equal to this ConsentId, false otherwise
   */
  public boolean equals(Object o) {
    if (this == o) return true;
    if (o == null || getClass() != o.getClass()) return false;
    var that = (ConsentId) o;
    return registeredClientId.equals(that.registeredClientId)
        && principalName.equals(that.principalName);
  }

  /**
   * Returns the hash code for this ConsentId.
   *
   * @return the hash code for this ConsentId
   */
  public int hashCode() {
    return Objects.hash(registeredClientId, principalName);
  }
}
