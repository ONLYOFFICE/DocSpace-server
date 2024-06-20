package com.asc.authorization.application.exception.client;

/** Exception thrown when a registered client does not have the necessary permissions. */
public class RegisteredClientPermissionException extends RuntimeException {

  /**
   * Constructs a new {@code RegisteredClientPermissionException} with the specified detail message.
   *
   * @param message the detail message.
   */
  public RegisteredClientPermissionException(String message) {
    super(message);
  }

  /**
   * Constructs a new {@code RegisteredClientPermissionException} with the specified detail message
   * and cause.
   *
   * @param message the detail message.
   * @param cause the cause of the exception.
   */
  public RegisteredClientPermissionException(String message, Throwable cause) {
    super(message, cause);
  }
}
