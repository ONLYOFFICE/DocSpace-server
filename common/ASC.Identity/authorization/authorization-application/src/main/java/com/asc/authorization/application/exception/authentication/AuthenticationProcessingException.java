package com.asc.authorization.application.exception.authentication;

import com.asc.authorization.application.security.oauth.errors.AuthenticationError;
import lombok.Getter;
import org.springframework.security.core.AuthenticationException;

/** Exception thrown when there is an error during the authentication process. */
@Getter
public class AuthenticationProcessingException extends AuthenticationException {

  /** The authentication error associated with this exception. */
  private final AuthenticationError error;

  /**
   * Constructs a new AuthenticationProcessingException with the specified detail message and error.
   *
   * @param error the authentication error
   * @param msg the detail message
   */
  public AuthenticationProcessingException(AuthenticationError error, String msg) {
    super(msg);
    this.error = error;
  }

  /**
   * Constructs a new AuthenticationProcessingException with the specified detail message, error,
   * and cause.
   *
   * @param error the authentication error
   * @param msg the detail message
   * @param cause the cause of the exception
   */
  public AuthenticationProcessingException(AuthenticationError error, String msg, Throwable cause) {
    super(msg, cause);
    this.error = error;
  }
}
