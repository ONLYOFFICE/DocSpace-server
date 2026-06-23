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
