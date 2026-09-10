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
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
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
      description =
          "The display name shown to the user on the consent screen. It has to be between 3 and "
              + "256 characters long.",
      example = "Updated Client",
      minLength = 3,
      maxLength = 256)
  private String name;

  /** The description of the client. */
  @Size(max = 255, message = "client description length is expected to be less than 256 characters")
  @Schema(
      description =
          "The free-text description shown next to the name on the consent screen, at most 255 "
              + "characters.",
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
      description =
          "The client logo as a data URI carrying base64 image data, shown on the consent screen. "
              + "Only png, jpeg, jpg and svg+xml are accepted.",
      example =
          "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==")
  private String logo;

  /** Indicates whether PKCE is allowed for the client. */
  @JsonProperty("allow_pkce")
  @Schema(
      description =
          "Whether the client may use PKCE. Turning it on lets the client authenticate with the "
              + "none method and prove itself with a code verifier instead of sending a secret, "
              + "which is what a client that cannot keep a secret needs.",
      example = "true")
  private boolean allowPkce;

  /** Indicates whether client is accessibly by third-party tenants * */
  @JsonProperty("is_public")
  @Schema(
      description =
          "Whether the client is offered to third-party tenants rather than only to the tenant "
              + "that registers it.",
      example = "false")
  private boolean isPublic;

  /** The allowed origins for the client. */
  @JsonProperty("allowed_origins")
  @NotNull(message = "allowed origins must not be null")
  @URLCollection
  @Size(
      min = 1,
      max = 12,
      message = "allowed origins must contain at least 1 and at most 12 addresses")
  @ArraySchema(
      arraySchema =
          @Schema(
              description =
                  "The web origins allowed to call the portal on behalf of this client, used for "
                      + "the CORS check. The set holds between 1 and 12 addresses.",
              example = "[\"http://example.com\"]"),
      schema = @Schema(type = "string", example = "http://example.com"))
  private Set<String> allowedOrigins;

  /** The redirect URIs for the client. */
  @JsonProperty("redirect_uris")
  @NotNull(message = "redirect uris must not be null")
  @URLCollection
  @Size(
      min = 1,
      max = 12,
      message = "redirect uris must contain at least 1 and at most 12 addresses")
  @ArraySchema(
      arraySchema =
          @Schema(
              description =
                  "The URIs an authorization code may be delivered to. An authorization request "
                      + "naming any other URI is refused, and the set holds between 1 and 12 "
                      + "addresses.",
              example = "[\"https://example.com/callback\"]"),
      schema = @Schema(type = "string", example = "https://example.com/callback"))
  private Set<String> redirectUris;

  /** The scopes for the client. This field cannot be empty. */
  @NotEmpty(message = "scopes field can not be empty")
  @ArraySchema(
      arraySchema =
          @Schema(
              description =
                  "The permissions the client may ask for, named as they appear in the tenant "
                      + "scope catalogue - for example files:read, rooms:write or openid. A client "
                      + "cannot request a scope that is not listed here.",
              example = "[\"files:read\", \"files:write\"]"),
      schema = @Schema(type = "string", example = "files:read"))
  private Set<String> scopes;
}
