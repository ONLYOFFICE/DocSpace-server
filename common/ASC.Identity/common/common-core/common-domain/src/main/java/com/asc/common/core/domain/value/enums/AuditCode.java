package com.asc.common.core.domain.value.enums;

import java.util.stream.Stream;

/** Represents an enumeration of audit codes used in the domain layer. */
public enum AuditCode {

  /** Audit code for creating a client. */
  CREATE_CLIENT(9901),

  /** Audit code for updating a client. */
  UPDATE_CLIENT(9902),

  /** Audit code for regenerating a secret. */
  REGENERATE_SECRET(9903),

  /** Audit code for deleting a client. */
  DELETE_CLIENT(9904),

  /** Audit code for changing a client's activation status. */
  CHANGE_CLIENT_ACTIVATION(9905),

  /** Audit code for revoking a user's client access. */
  REVOKE_USER_CLIENT(9906);

  private final int code;

  AuditCode(int code) {
    this.code = code;
  }

  /**
   * Returns the code associated with this audit code.
   *
   * @return The code associated with this audit code.
   */
  public int getCode() {
    return code;
  }

  /**
   * Returns the audit code associated with the given code.
   *
   * @param code The code to search for.
   * @return The audit code associated with the given code.
   * @throws IllegalArgumentException If no audit code is found with the given code.
   */
  public static AuditCode of(int code) {
    return Stream.of(AuditCode.values())
        .filter(p -> p.code == code)
        .findFirst()
        .orElseThrow(IllegalArgumentException::new);
  }
}
