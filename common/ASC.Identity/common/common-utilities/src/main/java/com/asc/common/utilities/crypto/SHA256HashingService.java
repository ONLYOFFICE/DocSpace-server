// (c) Copyright Ascensio System SIA 2009-2024
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
