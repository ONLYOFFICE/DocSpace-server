package com.asc.common.utilities.crypto;

/**
 * The EncryptionException class represents an exception that occurs during encryption operations.
 */
public class EncryptionException extends RuntimeException {
  /**
   * Constructs a new EncryptionException with the specified detail message.
   *
   * @param message the detail message (which is saved for later retrieval by the getMessage()
   *     method)
   */
  public EncryptionException(String message) {
    super(message);
  }

  /**
   * Constructs a new EncryptionException with the specified detail message and cause.
   *
   * @param message the detail message (which is saved for later retrieval by the getMessage()
   *     method)
   * @param cause the cause (which is saved for later retrieval by the getCause() method)
   */
  public EncryptionException(String message, Throwable cause) {
    super(message, cause);
  }
}
