package com.asc.common.core.domain.exception;

/**
 * Represents a general exception that occurs in the domain layer. This exception extends {@link
 * RuntimeException}.
 */
public class DomainException extends RuntimeException {

  /**
   * Constructs a new DomainException with the given message.
   *
   * @param message The detail message of the exception.
   */
  public DomainException(String message) {
    super(message);
  }

  /**
   * Constructs a new DomainException with the given message and cause.
   *
   * @param message The detail message of the exception.
   * @param cause The cause of the exception.
   */
  public DomainException(String message, Throwable cause) {
    super(message, cause);
  }
}
