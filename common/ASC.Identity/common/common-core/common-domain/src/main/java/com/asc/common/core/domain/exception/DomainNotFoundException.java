package com.asc.common.core.domain.exception;

/**
 * Represents an exception that occurs when a domain entity is not found. This exception extends
 * {@link DomainException}.
 */
public class DomainNotFoundException extends DomainException {

  /**
   * Constructs a new DomainNotFoundException with the given message.
   *
   * @param message The detail message of the exception.
   */
  public DomainNotFoundException(String message) {
    super(message);
  }

  /**
   * Constructs a new DomainNotFoundException with the given message and cause.
   *
   * @param message The detail message of the exception.
   * @param cause The cause of the exception.
   */
  public DomainNotFoundException(String message, Throwable cause) {
    super(message, cause);
  }
}
