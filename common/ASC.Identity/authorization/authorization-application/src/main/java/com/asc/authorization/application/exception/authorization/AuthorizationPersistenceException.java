package com.asc.authorization.application.exception.authorization;

import static com.asc.authorization.application.security.oauth.errors.AuthorizationError.ASC_IDENTITY_PERSISTENCE_ERROR;

import org.springframework.security.oauth2.core.OAuth2Error;

/** Exception thrown when there is an issue with persisting identity authorization. */
public class AuthorizationPersistenceException extends AuthorizationProcessingException {
  private static final OAuth2Error persistenceError =
      new OAuth2Error(ASC_IDENTITY_PERSISTENCE_ERROR.getCode());

  /**
   * Constructs a new AuthorizationPersistenceException with the specified detail message.
   *
   * @param message the detail message
   */
  public AuthorizationPersistenceException(String message) {
    super(persistenceError, message);
  }

  /**
   * Constructs a new AuthorizationPersistenceException with the specified cause.
   *
   * @param cause the cause of the exception
   */
  public AuthorizationPersistenceException(Throwable cause) {
    super(persistenceError, cause);
  }

  /**
   * Constructs a new AuthorizationPersistenceException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public AuthorizationPersistenceException(String message, Throwable cause) {
    super(persistenceError, message, cause);
  }
}
