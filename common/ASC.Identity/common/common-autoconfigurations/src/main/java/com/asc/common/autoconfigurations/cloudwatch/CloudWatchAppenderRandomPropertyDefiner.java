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

package com.asc.common.autoconfigurations.cloudwatch;

import ch.qos.logback.core.PropertyDefinerBase;
import java.security.SecureRandom;

/**
 * A Logback property definer that generates random string identifiers for CloudWatch log streams.
 *
 * <p>This class extends Logback's PropertyDefinerBase to generate random alphanumeric strings that
 * can be used as unique identifiers for log streams in AWS CloudWatch. The generated string is 36
 * characters long, composed of uppercase letters, lowercase letters, and digits.
 */
public class CloudWatchAppenderRandomPropertyDefiner extends PropertyDefinerBase {

  /**
   * The character set used for generating random strings. Includes uppercase letters (A-Z),
   * lowercase letters (a-z), and digits (0-9).
   */
  private static final String ALPHABET =
      "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

  /**
   * Secure random number generator for better entropy. Using SecureRandom instead of Random
   * provides higher quality randomness, which is beneficial even for non-security-critical
   * applications.
   */
  private static final SecureRandom RANDOM = new SecureRandom();

  /**
   * Generates a random alphanumeric string of 36 characters.
   *
   * <p>This method is called by the Logback framework when the property value is needed. It creates
   * a new random string each time it's called, which ensures uniqueness for each logging context
   * initialization.
   *
   * @return A 36-character random string composed of letters and digits
   */
  public String getPropertyValue() {
    StringBuilder sb = new StringBuilder(36);
    for (int i = 0; i < 36; i++) sb.append(ALPHABET.charAt(RANDOM.nextInt(ALPHABET.length())));
    return sb.toString();
  }
}
