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

package com.asc.registration.application.transfer;

import java.util.Set;

/**
 * ValidationErrorCode provides error codes based on validation constraint types. These codes can be
 * used by the frontend for localization of error messages.
 *
 * <p>Error codes:
 *
 * <ul>
 *   <li>{@code ErrorWrongURL} - Invalid URL format (Pattern/URLCollection on URL fields)
 *   <li>{@code ErrorName} - Invalid length (Size validation)
 *   <li>{@code EmptyFieldError} - Field is empty or null (NotEmpty/NotNull/NotBlank)
 *   <li>{@code ErrorPattern} - Invalid pattern/format (Pattern validation on non-URL fields)
 * </ul>
 */
public final class ValidationErrorCodeResponse {
  public static final String ERROR_NAME = "ErrorName";
  public static final String ERROR_PATTERN = "ErrorPattern";
  public static final String ERROR_WRONG_URL = "ErrorWrongURL";
  public static final String EMPTY_FIELD_ERROR = "EmptyFieldError";
  public static final String ERROR_INVALID_SCOPE = "ErrorInvalidScope";

  private static final Set<String> EMPTY_CONSTRAINTS = Set.of("NotEmpty", "NotNull", "NotBlank");
  private static final Set<String> SIZE_CONSTRAINTS = Set.of("Size", "Min", "Max");
  private static final Set<String> URL_CONSTRAINTS = Set.of("URL", "URLCollection");

  private ValidationErrorCodeResponse() {}

  /**
   * Gets the error code based on the validation constraint code and message.
   *
   * @param constraintCode the validation constraint code (e.g., "NotEmpty", "Pattern", "Size")
   * @param message the validation error message
   * @return the appropriate error code
   */
  public static String getErrorCode(String constraintCode, String message) {
    if (constraintCode != null) {
      if (EMPTY_CONSTRAINTS.contains(constraintCode)) return EMPTY_FIELD_ERROR;

      if (SIZE_CONSTRAINTS.contains(constraintCode)) return ERROR_NAME;

      if (URL_CONSTRAINTS.contains(constraintCode)) {
        return ERROR_WRONG_URL;
      }

      if ("Pattern".equals(constraintCode)) {
        if (message != null && message.toLowerCase().contains("url")) return ERROR_WRONG_URL;
        return ERROR_PATTERN;
      }
    }

    return ERROR_PATTERN;
  }

  /**
   * Normalizes a field name to snake_case format for consistent JSON property naming.
   *
   * @param fieldName the field name to normalize
   * @return the normalized field name in snake_case
   */
  public static String normalizeFieldName(String fieldName) {
    if (fieldName == null || fieldName.isEmpty()) return fieldName;

    var result = new StringBuilder();

    for (var i = 0; i < fieldName.length(); i++) {
      var c = fieldName.charAt(i);
      if (Character.isUpperCase(c)) {
        if (i > 0) result.append('_');
        result.append(Character.toLowerCase(c));
      } else {
        result.append(c);
      }
    }

    return result.toString();
  }
}
