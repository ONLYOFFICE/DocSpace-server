package com.asc.authorization.application.exception.authorization;

import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2Error;

/** Exception thrown when there is an issue with processing authorization. */
public class AuthorizationProcessingException extends OAuth2AuthenticationException {

  /**
   * Constructs a new AuthorizationProcessingException with the specified OAuth2Error and detail
   * message.
   *
   * @param error the OAuth2Error describing the exception
   * @param message the detail message
   */
  public AuthorizationProcessingException(OAuth2Error error, String message) {
    super(error, message);
  }

  /**
   * Constructs a new AuthorizationProcessingException with the specified OAuth2Error and cause.
   *
   * @param error the OAuth2Error describing the exception
   * @param cause the cause of the exception
   */
  public AuthorizationProcessingException(OAuth2Error error, Throwable cause) {
    super(error, cause);
  }

  /**
   * Constructs a new AuthorizationProcessingException with the specified OAuth2Error, detail
   * message, and cause.
   *
   * @param error the OAuth2Error describing the exception
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public AuthorizationProcessingException(OAuth2Error error, String message, Throwable cause) {
    super(error, message, cause);
  }
}
