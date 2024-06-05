package com.asc.registration.core.domain.exception;

import com.asc.common.core.domain.exception.DomainException;

/**
 * ClientDomainException represents an exception that is specific to the Client domain. It extends
 * the {@link DomainException} class and provides constructors to create exceptions with a message
 * and an optional cause.
 */
public class ClientDomainException extends DomainException {

  /**
   * Constructs a ClientDomainException with the specified detail message.
   *
   * @param message the detail message
   */
  public ClientDomainException(String message) {
    super(message);
  }

  /**
   * Constructs a ClientDomainException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public ClientDomainException(String message, Throwable cause) {
    super(message, cause);
  }
}
