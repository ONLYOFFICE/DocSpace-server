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

import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import java.util.List;

/**
 * ValidationErrorResponse represents a structured response for validation errors. It contains a
 * list of field-specific validation errors that can be used by the client to display error messages
 * next to the corresponding form fields.
 *
 * @param error the error type identifier
 * @param message the general error message
 * @param errors the list of field-specific validation errors
 */
@Schema(description = "Response containing validation errors")
public record ValidationErrorResponse(
    @JsonProperty("error")
        @Schema(description = "Error type identifier", example = "ValidationError")
        String error,
    @JsonProperty("message")
        @Schema(description = "General error message", example = "Validation failed")
        String message,
    @JsonProperty("errors") @Schema(description = "List of field specific validation errors")
        List<FieldError> errors) {

  /**
   * FieldError represents a validation error for a specific field. It contains the field name, the
   * error code that can be used for localization, and a human-readable error message.
   *
   * @param field the name of the field that failed validation
   * @param code the error code for localization purposes
   * @param message the human-readable error message
   */
  @Schema(description = "Field specific validation error")
  public record FieldError(
      @JsonProperty("field")
          @Schema(
              description = "The name of the field that failed validation",
              example = "policy_url")
          String field,
      @JsonProperty("code")
          @Schema(
              description = "Error code for localization purposes",
              example = "InvalidPolicyUrl")
          String code,
      @JsonProperty("message")
          @Schema(
              description = "Human readable error message",
              example = "policy url is expected to be passed as url")
          String message) {}
}
