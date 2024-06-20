package com.asc.authorization.application.exception.authorization;

import static com.asc.authorization.application.security.oauth.errors.AuthorizationError.ASC_IDENTITY_RETRIEVAL_ERROR;

import org.springframework.security.oauth2.core.OAuth2Error;

/** Exception thrown when there is an issue retrieving authorization data. */
public class AuthorizationRetrievalException extends AuthorizationProcessingException {
  private static final OAuth2Error retrievalError =
      new OAuth2Error(ASC_IDENTITY_RETRIEVAL_ERROR.getCode());

  /**
   * Constructs a new AuthorizationRetrievalException with the specified detail message.
   *
   * @param message the detail message
   */
  public AuthorizationRetrievalException(String message) {
    super(retrievalError, message);
  }

  /**
   * Constructs a new AuthorizationRetrievalException with the specified cause.
   *
   * @param cause the cause of the exception
   */
  public AuthorizationRetrievalException(Throwable cause) {
    super(retrievalError, cause);
  }

  /**
   * Constructs a new AuthorizationRetrievalException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public AuthorizationRetrievalException(String message, Throwable cause) {
    super(retrievalError, message, cause);
  }
}
