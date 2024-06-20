package com.asc.registration.core.domain.exception;

/** Represents an exception indicating that a scope was not found. */
public class ScopeNotFoundException extends ScopeDomainException {
  /**
   * Constructs a ScopeNotFoundException with the specified detail message.
   *
   * @param message the detail message
   */
  public ScopeNotFoundException(String message) {
    super(message);
  }

  /**
   * Constructs a ScopeNotFoundException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public ScopeNotFoundException(String message, Throwable cause) {
    super(message, cause);
  }
}
