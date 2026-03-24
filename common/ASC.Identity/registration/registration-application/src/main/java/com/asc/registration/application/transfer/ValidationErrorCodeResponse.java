// (c) Copyright Ascensio System SIA 2009-2026
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
