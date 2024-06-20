package com.asc.common.utilities.crypto;

/** Exception thrown when a decryption operation fails. */
public class DecryptionException extends RuntimeException {

  /**
   * Constructs a new DecryptionException with the specified detail message.
   *
   * @param message the detail message
   */
  public DecryptionException(String message) {
    super(message);
  }

  /**
   * Constructs a new DecryptionException with the specified detail message and cause.
   *
   * @param message the detail message
   * @param cause the cause of the exception
   */
  public DecryptionException(String message, Throwable cause) {
    super(message, cause);
  }
}
