package com.asc.common.core.domain.exception;

/**
 * Represents an exception that occurs when a consent is not found in the domain layer. This
 * exception extends {@link DomainNotFoundException}.
 */
public class ConsentNotFoundException extends DomainNotFoundException {

  /**
   * Constructs a new ConsentNotFoundException with the given message.
   *
   * @param message The detail message of the exception.
   */
  public ConsentNotFoundException(String message) {
    super(message);
  }

  /**
   * Constructs a new ConsentNotFoundException with the given message and cause.
   *
   * @param message The detail message of the exception.
   * @param cause The cause of the exception.
   */
  public ConsentNotFoundException(String message, Throwable cause) {
    super(message, cause);
  }
}
