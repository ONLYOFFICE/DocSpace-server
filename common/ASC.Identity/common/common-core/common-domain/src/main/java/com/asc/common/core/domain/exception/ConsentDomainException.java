package com.asc.common.core.domain.exception;

/**
 * Represents an exception that occurs during consent management in the domain layer. This exception
 * extends {@link DomainException}.
 */
public class ConsentDomainException extends DomainException {

  /**
   * Constructs a new ConsentDomainException with the given message.
   *
   * @param message The detail message of the exception.
   */
  public ConsentDomainException(String message) {
    super(message);
  }

  /**
   * Constructs a new ConsentDomainException with the given message and cause.
   *
   * @param message The detail message of the exception.
   * @param cause The cause of the exception.
   */
  public ConsentDomainException(String message, Throwable cause) {
    super(message, cause);
  }
}
