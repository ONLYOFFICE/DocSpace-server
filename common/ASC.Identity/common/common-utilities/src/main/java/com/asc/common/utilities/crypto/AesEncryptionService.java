package com.asc.common.utilities.crypto;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.security.InvalidAlgorithmParameterException;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.security.SecureRandom;
import java.security.spec.InvalidKeySpecException;
import java.util.Arrays;
import java.util.Base64;
import javax.crypto.Cipher;
import javax.crypto.NoSuchPaddingException;
import javax.crypto.SecretKey;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.PBEKeySpec;
import javax.crypto.spec.SecretKeySpec;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;

/** Provides AES encryption and decryption services using the AES/GCM/NoPadding algorithm. */
@Slf4j
public class AesEncryptionService implements EncryptionService {
  private static final String ALGORITHM = "AES/GCM/NoPadding";
  private static final String FACTORY_INSTANCE = "PBKDF2WithHmacSHA256";
  private static final int TAG_LENGTH_BIT = 128;
  private static final int IV_LENGTH_BYTE = 12;
  private static final int SALT_LENGTH_BYTE = 16;
  private static final String ALGORITHM_TYPE = "AES";
  private static final int KEY_LENGTH = 128;
  private static final int ITERATION_COUNT = 1200; // Min 1000
  private static final Charset UTF_8 = StandardCharsets.UTF_8;

  private final String secret;

  /**
   * Constructs a new AesEncryptionService with the specified secret key.
   *
   * @param secret the secret key to use for encryption and decryption
   */
  public AesEncryptionService(String secret) {
    this.secret = secret;
  }

  /**
   * Generates a random nonce of the specified length.
   *
   * @param length the length of the nonce to generate
   * @return the generated nonce as a byte array
   */
  private byte[] getRandomNonce(int length) {
    var nonce = new byte[length];
    new SecureRandom().nextBytes(nonce);

    MDC.put("nonce", Arrays.toString(nonce));
    log.debug("Generating random nonce");
    MDC.clear();

    return nonce;
  }

  /**
   * Generates a secret key using the specified password and salt.
   *
   * @param password the password to use for generating the secret key
   * @param salt the salt to use for generating the secret key
   * @return the generated secret key
   * @throws NoSuchAlgorithmException if the algorithm is not available in the environment
   * @throws InvalidKeySpecException if the key specifications are invalid
   */
  private SecretKey getSecretKey(String password, byte[] salt)
      throws NoSuchAlgorithmException, InvalidKeySpecException {
    MDC.put("password", password);
    log.debug("Generating secret key");
    MDC.clear();

    var spec = new PBEKeySpec(password.toCharArray(), salt, ITERATION_COUNT, KEY_LENGTH);
    var factory = SecretKeyFactory.getInstance(FACTORY_INSTANCE);
    return new SecretKeySpec(factory.generateSecret(spec).getEncoded(), ALGORITHM_TYPE);
  }

  /**
   * Initializes a new cipher with the specified mode, secret key, and initialization vector (IV).
   *
   * @param mode the cipher mode (e.g., Cipher.ENCRYPT_MODE or Cipher.DECRYPT_MODE)
   * @param secretKey the secret key to use for the cipher
   * @param iv the initialization vector to use for the cipher
   * @return the initialized cipher
   * @throws InvalidKeyException if the key is invalid
   * @throws InvalidAlgorithmParameterException if the algorithm parameters are invalid
   * @throws NoSuchPaddingException if the padding scheme is not available
   * @throws NoSuchAlgorithmException if the algorithm is not available in the environment
   */
  private Cipher initCipher(int mode, SecretKey secretKey, byte[] iv)
      throws InvalidKeyException,
          InvalidAlgorithmParameterException,
          NoSuchPaddingException,
          NoSuchAlgorithmException {
    MDC.put("algorithm", ALGORITHM);
    MDC.put("tag", String.valueOf(TAG_LENGTH_BIT));
    log.debug("Initializing a new cipher");
    MDC.clear();

    var cipher = Cipher.getInstance(ALGORITHM);
    cipher.init(mode, secretKey, new GCMParameterSpec(TAG_LENGTH_BIT, iv));
    return cipher;
  }

  /**
   * Encrypts the specified plain text using AES encryption.
   *
   * @param plainText the plain text to encrypt
   * @return the encrypted text as a Base64 encoded string
   * @throws EncryptionException if an error occurs during encryption
   */
  public String encrypt(String plainText) throws EncryptionException {
    MDC.put("plain_text", plainText);
    log.debug("Trying to encrypt plain message");
    try {
      var salt = getRandomNonce(SALT_LENGTH_BYTE);
      var secretKey = getSecretKey(secret, salt);
      var iv = getRandomNonce(IV_LENGTH_BYTE);
      var cipher = initCipher(Cipher.ENCRYPT_MODE, secretKey, iv);
      var encryptedMessageByte = cipher.doFinal(plainText.getBytes(UTF_8));
      var cipherByte =
          ByteBuffer.allocate(iv.length + salt.length + encryptedMessageByte.length)
              .put(iv)
              .put(salt)
              .put(encryptedMessageByte)
              .array();

      var encrypted = Base64.getEncoder().encodeToString(cipherByte);

      MDC.put("cipher_text", encrypted);
      log.debug("Managed to encrypt plain text message");
      return encrypted;
    } catch (Exception e) {
      throw new EncryptionException(e.getMessage());
    } finally {
      MDC.clear();
    }
  }

  /**
   * Decrypts the specified cipher text using AES decryption.
   *
   * @param cipherText the cipher text to decrypt
   * @return the decrypted plain text
   * @throws DecryptionException if an error occurs during decryption
   */
  public String decrypt(String cipherText) throws DecryptionException {
    MDC.put("cipher_text", cipherText);
    log.debug("Trying to decrypt cipher message");

    try {
      var decodedCipherByte = Base64.getDecoder().decode(cipherText.getBytes(UTF_8));
      var byteBuffer = ByteBuffer.wrap(decodedCipherByte);

      var iv = new byte[IV_LENGTH_BYTE];
      byteBuffer.get(iv);

      var salt = new byte[SALT_LENGTH_BYTE];
      byteBuffer.get(salt);

      var encryptedByte = new byte[byteBuffer.remaining()];
      byteBuffer.get(encryptedByte);

      var secretKey = getSecretKey(secret, salt);
      var cipher = initCipher(Cipher.DECRYPT_MODE, secretKey, iv);

      var decryptedMessageByte = cipher.doFinal(encryptedByte);

      var decrypted = new String(decryptedMessageByte, UTF_8);

      MDC.put("plain_text", decrypted);
      log.debug("Decrypted cipher message");

      return decrypted;
    } catch (Exception e) {
      throw new DecryptionException(e.getMessage());
    } finally {
      MDC.clear();
    }
  }
}
