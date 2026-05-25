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

import com.asc.common.utilities.validation.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.io.Serializable;
import java.util.Set;
import lombok.*;

/**
 * UpdateClientRequest is a data transfer object (DTO) used in the REST layer. It represents a
 * request to update an existing tenant client. This class contains the necessary information to
 * update a client for a tenant. It implements {@link Serializable} to allow instances of this class
 * to be serialized.
 *
 * <p>The class is annotated with Lombok annotations to generate boilerplate code:
 *
 * <ul>
 *   <li>{@link Getter} - Generates getter methods for all fields.
 *   <li>{@link Setter} - Generates setter methods for all fields.
 *   <li>{@link Builder} - Implements the builder pattern for object creation.
 *   <li>{@link NoArgsConstructor} - Generates a no-arguments constructor.
 *   <li>{@link AllArgsConstructor} - Generates an all-arguments constructor.
 * </ul>
 *
 * <p>The class also includes validation annotations to ensure that the input data meets the
 * expected format:
 *
 * <ul>
 *   <li>{@link NotBlank} - Ensures that the field is not blank.
 *   <li>{@link Pattern} - Validates that the field matches the specified regular expression.
 * </ul>
 *
 * Example usage:
 *
 * <pre>{@code
 * UpdateClientRequest request = UpdateClientRequest.builder()
 *     .name("Updated Client")
 *     .description("Updated description of the client")
 *     .logo("data:image/png;base64,...")
 *     .allowPkce(true)
 *     .allowedOrigins(Set.of("http://allowed.origin"))
 *     .build();
 * }</pre>
 *
 * @see Serializable
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Schema(description = "Request to update an existing tenant client")
public class UpdateClientRequest implements Serializable {
  /**
   * The name of the client. The client name length is expected to be between 3 and 256 characters.
   */
  @NotBlank(message = "client name must not be empty")
  @Size(
      min = 3,
      max = 256,
      message = "client name length is expected to be between 3 and 256 characters")
  @Schema(
      description = "The name of the client",
      example = "Updated Client",
      minLength = 3,
      maxLength = 256)
  private String name;

  /** The description of the client. */
  @Size(max = 255, message = "client description length is expected to be less than 256 characters")
  @Schema(
      description = "The description of the client",
      example = "Updated description of the client",
      maxLength = 255)
  private String description;

  /**
   * The logo of the client in base64 format. The client logo is expected to be passed as base64.
   * This field must not be blank.
   */
  @NotBlank(message = "client logo must not be empty")
  @Pattern(
      regexp = "^data:image\\/(?:png|jpeg|jpg|svg\\+xml);base64,.*.{1,}",
      message = "client logo is expected to be passed as base64")
  @Schema(
      description = "The logo of the client in base64 format",
      example = "data:image/png;base64,...")
  private String logo;

  /** Indicates whether PKCE is allowed for the client. */
  @JsonProperty("allow_pkce")
  @Schema(description = "Indicates whether PKCE is allowed for the client", example = "true")
  private boolean allowPkce;

  /** Indicates whether client is accessibly by third-party tenants * */
  @JsonProperty("is_public")
  @Schema(
      description = "Indicates whether client is accessible by third-party tenants",
      example = "false")
  private boolean isPublic;

  /** The allowed origins for the client. */
  @JsonProperty("allowed_origins")
  @URLCollection
  @Size(
      min = 1,
      max = 12,
      message = "allowed origins must contain at least 1 and at most 12 addresses")
  @Schema(
      description = "The allowed origins for the client",
      example = "[\"http://allowed.origin\"]")
  private Set<String> allowedOrigins;
}
