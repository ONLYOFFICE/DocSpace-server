package com.asc.registration.core.domain.exception;

import com.asc.common.core.domain.exception.DomainException;

/** Represents a domain exception specific to Scope operations. */
public class ScopeDomainException extends DomainException {
  /**
   * Constructs a ScopeDomainException with the specified detail message.
   *
   * @param message the detail message
   */
  public ScopeDomainException(String message) {
    super(message);
  }

  /**
   * Constructs a ScopeDomainException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public ScopeDomainException(String message, Throwable cause) {
    super(message, cause);
  }
}
