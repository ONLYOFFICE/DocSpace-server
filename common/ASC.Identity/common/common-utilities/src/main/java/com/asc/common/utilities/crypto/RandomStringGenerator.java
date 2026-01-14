// (c) Copyright Ascensio System SIA 2009-2025
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

import java.security.SecureRandom;

/**
 * Utility class for generating cryptographically secure random strings.
 *
 * <p>This class provides methods to generate random strings using a specified character set and
 * length. It uses {@link SecureRandom} to ensure cryptographic strength suitable for generating
 * security-sensitive identifiers.
 */
public final class RandomStringGenerator {
  private static final String VALID_CHARS =
      "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  private static final SecureRandom SECURE_RANDOM = new SecureRandom();

  private RandomStringGenerator() {
    throw new UnsupportedOperationException("Utility class should not be instantiated");
  }

  /**
   * Generates a random alphanumeric string of the specified length.
   *
   * @param length the length of the random string to generate
   * @return a random alphanumeric string of the specified length
   * @throws IllegalArgumentException if length is negative or zero
   */
  public static String generate(int length) {
    if (length <= 0) throw new IllegalArgumentException("Length must be positive");

    var builder = new StringBuilder(length);
    for (var i = 0; i < length; i++)
      builder.append(VALID_CHARS.charAt(SECURE_RANDOM.nextInt(VALID_CHARS.length())));
    return builder.toString();
  }
}
