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

package com.asc.common.utilities.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;
import java.util.Base64;

/**
 * Validator for the {@link LogoSize} annotation. Ensures that a base64-encoded string representing
 * a logo does not exceed the specified byte size and character length.
 */
public class LogoSizeValidator implements ConstraintValidator<LogoSize, String> {
  private int maxLength;
  private long maxBytes;

  /**
   * Initializes the validator with the parameters defined in the {@link LogoSize} annotation.
   *
   * @param constraintAnnotation the annotation instance for a given constraint declaration
   */
  public void initialize(LogoSize constraintAnnotation) {
    this.maxLength = constraintAnnotation.maxLength();
    this.maxBytes = constraintAnnotation.maxBytes();
  }

  /**
   * Validates that the provided base64-encoded logo string does not exceed the specified byte size
   * and character length.
   *
   * @param value the base64-encoded string to validate
   * @param context the context in which the constraint is evaluated
   * @return {@code true} if the string is valid; {@code false} otherwise
   */
  public boolean isValid(String value, ConstraintValidatorContext context) {
    if (value == null || value.isEmpty()) {
      return true;
    }

    // Early rejection for excessively long base64 strings
    if (value.length() > maxLength) {
      return false;
    }

    try {
      // Remove base64 metadata prefix if present (e.g., "data:image/png;base64,")
      int base64Index = value.indexOf(",");
      if (base64Index >= 0) value = value.substring(base64Index + 1);

      byte[] decodedBytes = Base64.getDecoder().decode(value);
      return decodedBytes.length <= maxBytes;
    } catch (IllegalArgumentException e) {
      return false;
    }
  }
}
