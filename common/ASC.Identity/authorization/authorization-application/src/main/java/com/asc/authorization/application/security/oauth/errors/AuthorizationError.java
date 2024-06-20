package com.asc.authorization.application.security.oauth.errors;

import lombok.Getter;

/** Enum representing various authorization errors. */
@Getter
public enum AuthorizationError {

  /** Error indicating an issue with cleaning up identity authorization. */
  ASC_IDENTITY_CLEANUP_ERROR("identity_authorization_cleanup_error"),

  /** Error indicating an issue with persisting identity authorization. */
  ASC_IDENTITY_PERSISTENCE_ERROR("identity_authorization_persistence_error"),

  /** Error indicating an issue with retrieving identity authorization. */
  ASC_IDENTITY_RETRIEVAL_ERROR("identity_authorization_retrieval_error");

  /** The error code associated with the authorization error. */
  private final String code;

  /**
   * Constructs an AuthorizationError with the specified error code.
   *
   * @param code the error code
   */
  AuthorizationError(String code) {
    this.code = code;
  }
}
