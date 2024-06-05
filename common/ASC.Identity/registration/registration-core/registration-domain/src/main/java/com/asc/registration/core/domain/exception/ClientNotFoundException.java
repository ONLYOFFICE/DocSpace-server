package com.asc.registration.core.domain.exception;

import com.asc.common.core.domain.exception.DomainNotFoundException;

/**
 * ClientNotFoundException represents an exception that is thrown when a client is not found in the
 * domain. It extends the {@link DomainNotFoundException} class and provides constructors to create
 * exceptions with a message and an optional cause.
 */
public class ClientNotFoundException extends DomainNotFoundException {

  /**
   * Constructs a ClientNotFoundException with the specified detail message.
   *
   * @param message the detail message
   */
  public ClientNotFoundException(String message) {
    super(message);
  }

  /**
   * Constructs a ClientNotFoundException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public ClientNotFoundException(String message, Throwable cause) {
    super(message, cause);
  }
}
