package com.asc.common.utilities.cipher;

/** The EncryptionService interface provides methods for encrypting and decrypting text. */
public interface EncryptionService {

  /**
   * Encrypts the given text.
   *
   * @param text the text to be encrypted
   * @return the encrypted cipher text
   * @throws EncryptionException if an error occurs during encryption
   */
  String encrypt(String text) throws EncryptionException;

  /**
   * Decrypts the given cipher text.
   *
   * @param cipher the cipher text to be decrypted
   * @return the decrypted text
   * @throws DecryptionException if an error occurs during decryption
   */
  String decrypt(String cipher) throws DecryptionException;
}
