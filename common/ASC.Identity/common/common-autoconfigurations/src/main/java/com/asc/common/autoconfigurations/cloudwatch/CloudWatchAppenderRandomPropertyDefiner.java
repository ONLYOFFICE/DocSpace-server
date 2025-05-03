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
//

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
