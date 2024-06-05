package com.asc.registration.core.domain.exception;

import com.asc.common.core.domain.exception.DomainException;

/**
 * ConsentDomainException represents an exception that is specific to the Consent domain. It extends
 * the {@link DomainException} class and provides constructors to create exceptions with a message
 * and an optional cause.
 */
public class ConsentDomainException extends DomainException {

  /**
   * Constructs a ConsentDomainException with the specified detail message.
   *
   * @param message the detail message
   */
  public ConsentDomainException(String message) {
    super(message);
  }

  /**
   * Constructs a ConsentDomainException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public ConsentDomainException(String message, Throwable cause) {
    super(message, cause);
  }
}
