// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.common.utilities.crypto;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import org.springframework.stereotype.Component;

/** Provides hashing functionality using the SHA-256 algorithm. */
@Component
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
