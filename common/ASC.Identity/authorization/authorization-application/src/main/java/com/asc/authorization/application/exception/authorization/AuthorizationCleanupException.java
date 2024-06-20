package com.asc.authorization.application.exception.authorization;

import static com.asc.authorization.application.security.oauth.errors.AuthorizationError.ASC_IDENTITY_CLEANUP_ERROR;

import org.springframework.security.oauth2.core.OAuth2Error;

/** Exception thrown when there is an issue with cleaning up identity authorization. */
public class AuthorizationCleanupException extends AuthorizationProcessingException {
  private static final OAuth2Error cleanupError =
      new OAuth2Error(ASC_IDENTITY_CLEANUP_ERROR.getCode());

  /**
   * Constructs a new AuthorizationCleanupException with the specified detail message.
   *
   * @param message the detail message
   */
  public AuthorizationCleanupException(String message) {
    super(cleanupError, message);
  }

  /**
   * Constructs a new AuthorizationCleanupException with the specified cause.
   *
   * @param cause the cause of the exception
   */
  public AuthorizationCleanupException(Throwable cause) {
    super(cleanupError, cause);
  }

  /**
   * Constructs a new AuthorizationCleanupException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public AuthorizationCleanupException(String message, Throwable cause) {
    super(cleanupError, message, cause);
  }
}
