package com.asc.common.core.domain.exception;

/**
 * Represents an exception that occurs during auditing in the domain layer. This exception extends
 * {@link DomainException}.
 */
public class AuditDomainException extends DomainException {

  /**
   * Constructs a new AuditDomainException with the given message.
   *
   * @param message The detail message of the exception.
   */
  public AuditDomainException(String message) {
    super(message);
  }

  /**
   * Constructs a new AuditDomainException with the given message and cause.
   *
   * @param message The detail message of the exception.
   * @param cause The cause of the exception.
   */
  public AuditDomainException(String message, Throwable cause) {
    super(message, cause);
  }
}
