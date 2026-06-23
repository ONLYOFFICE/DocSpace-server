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
