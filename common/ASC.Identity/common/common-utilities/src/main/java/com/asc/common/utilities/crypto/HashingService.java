package com.asc.common.utilities.crypto;

public interface HashingService {
  /**
   * Hashes the given data.
   *
   * @param data the data to hash
   * @return the hashed data as a hex string
   */
  String hash(String data);

  /**
   * Verifies if the given data matches the hashed data.
   *
   * @param data the original data
   * @param hashedData the hashed data to compare with
   * @return true if the data matches the hashed data, false otherwise
   */
  boolean verify(String data, String hashedData);
}
