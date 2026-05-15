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
