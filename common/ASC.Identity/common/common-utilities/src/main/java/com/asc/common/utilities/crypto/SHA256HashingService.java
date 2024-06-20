package com.asc.common.utilities.crypto;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;

/** Provides hashing functionality using the SHA-256 algorithm. */
public class SHA256HashingService implements HashingService {
  private static final String ALGORITHM = "SHA-256";

  /**
   * Hashes the given data using the SHA-256 algorithm.
   *
   * @param data the data to hash
   * @return the hashed data as a hexadecimal string, or null if an error occurs
   */
  public String hash(String data) {
    try {
      var digest = MessageDigest.getInstance(ALGORITHM);
      var hash = digest.digest(data.getBytes(StandardCharsets.UTF_8));
      return bytesToHex(hash);
    } catch (Exception e) {
      return null;
    }
  }

  /**
   * Verifies that the given data matches the given hashed data.
   *
   * @param data the data to verify
   * @param hashedData the hashed data to compare against
   * @return true if the data matches the hashed data, false otherwise
   */
  public boolean verify(String data, String hashedData) {
    var newHash = hash(data);
    return newHash.equals(hashedData);
  }

  /**
   * Converts a byte array to a hexadecimal string.
   *
   * @param bytes the byte array to convert
   * @return the hexadecimal string representation of the byte array
   */
  private String bytesToHex(byte[] bytes) {
    var hexString = new StringBuilder();
    for (byte b : bytes) {
      var hex = Integer.toHexString(0xff & b);
      if (hex.length() == 1) hexString.append('0');
      hexString.append(hex);
    }
    return hexString.toString();
  }
}
