package com.asc.common.utilities.cipher;

/**
 * The NoOpEncryptionService class provides a no-operation implementation of the EncryptionService
 * interface. This implementation simply returns the input text or cipher without performing any
 * encryption or decryption.
 */
public class NoOpEncryptionService implements EncryptionService {

  /**
   * Encrypts the given text. This implementation simply returns the input text as is.
   *
   * @param text the text to be encrypted
   * @return the input text as is (no encryption is performed)
   * @throws EncryptionException if an error occurs during encryption (not thrown in this
   *     implementation)
   */
  public String encrypt(String text) throws EncryptionException {
    return text;
  }

  /**
   * Decrypts the given cipher text. This implementation simply returns the input cipher as is.
   *
   * @param cipher the cipher text to be decrypted
   * @return the input cipher as is (no decryption is performed)
   * @throws DecryptionException if an error occurs during decryption (not thrown in this
   *     implementation)
   */
  public String decrypt(String cipher) throws DecryptionException {
    return cipher;
  }
}
