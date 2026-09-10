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

package com.asc.common.service.transfer.response;

import com.fasterxml.jackson.annotation.JsonGetter;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Schema;
import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Set;
import lombok.*;

/**
 * ClientResponse is a Data Transfer Object (DTO) used to transfer detailed client information in
 * responses. This class encapsulates all relevant information about a client, including
 * identification, authentication methods, and various URLs associated with the client.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ClientResponse implements Serializable {

  /** The name of the client. */
  @Schema(
      description =
          "The display name shown to the user on the consent screen, between 3 and 256 "
              + "characters.",
      example = "Example Name")
  private String name;

  /** The unique identifier of the client. */
  @JsonProperty("client_id")
  @Schema(
      description =
          "The generated identifier of the client, sent as client_id in every OAuth2 request. It "
              + "is assigned when the client is registered and never changes afterwards.",
      example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
  private String clientId;

  /** The client secret. Included only if it is not null. */
  @JsonProperty("client_secret")
  @JsonInclude(JsonInclude.Include.NON_NULL)
  @Schema(
      description =
          "The client secret, which the client presents at the token endpoint when it "
              + "authenticates with client_secret_post. It is omitted from the response rather than "
              + "sent as null when the client has none.",
      example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
  private String clientSecret;

  /** The description of the client. */
  @Schema(
      description =
          "The free-text description shown next to the name on the consent screen, at most 255 "
              + "characters.",
      example = "Example Description")
  private String description;

  /** The website URL of the client. */
  @JsonProperty("website_url")
  @Schema(
      description = "The URL of the client home page, offered to the user before they consent.",
      example = "http://example.com")
  private String websiteUrl;

  /** The terms of service URL of the client. */
  @JsonProperty("terms_url")
  @Schema(
      description = "The URL of the client terms of service, linked from the consent screen.",
      example = "http://example.com")
  private String termsUrl;

  /** The privacy policy URL of the client. */
  @JsonProperty("policy_url")
  @Schema(
      description = "The URL of the client privacy policy, linked from the consent screen.",
      example = "http://example.com")
  private String policyUrl;

  /** The logo of the client. */
  @JsonProperty("logo")
  @Schema(
      description =
          "The client logo as a data URI carrying base64 image data, shown on the consent screen. "
              + "Only png, jpeg, jpg and svg+xml are accepted, the whole string may not exceed "
              + "2000000 characters and the decoded image may not exceed 256000 bytes.",
      example =
          "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==")
  private String logo;

  /** The authentication methods supported by the client. */
  @JsonProperty("authentication_methods")
  @ArraySchema(
      arraySchema =
          @Schema(
              description =
                  "How the client authenticates itself at the token endpoint: client_secret_post "
                      + "for a confidential client that sends its secret, none for a public client "
                      + "that proves itself with PKCE instead.",
              example = "[\"client_secret_post\"]"),
      schema = @Schema(type = "string", example = "client_secret_post"))
  private Set<String> authenticationMethods;

  /** The tenant ID associated with the client. */
  @Schema(
      description =
          "The identifier of the portal the client belongs to. A client is visible only inside "
              + "its own tenant, apart from the unauthenticated public info read.",
      example = "1")
  private long tenant;

  /** The redirect URIs registered for the client. */
  @JsonProperty("redirect_uris")
  @ArraySchema(
      arraySchema =
          @Schema(
              description =
                  "The URIs an authorization code may be delivered to. An authorization request "
                      + "naming any other URI is refused, and the set holds between 1 and 12 "
                      + "addresses.",
              example = "[\"https://example.com\"]"),
      schema = @Schema(type = "string", example = "https://example.com"))
  private Set<String> redirectUris;

  /** The allowed origins for the client. */
  @JsonProperty("allowed_origins")
  @ArraySchema(
      arraySchema =
          @Schema(
              description =
                  "The web origins allowed to call the portal on behalf of this client, used for "
                      + "the CORS check. The set holds between 1 and 12 addresses.",
              example = "[\"https://example.com\"]"),
      schema = @Schema(type = "string", example = "https://example.com"))
  private Set<String> allowedOrigins;

  /** The logout redirect URIs registered for the client. */
  @JsonProperty("logout_redirect_uris")
  @ArraySchema(
      arraySchema =
          @Schema(
              description = "The URIs the user may be sent back to once they have logged out.",
              example = "[\"https://example.com\"]"),
      schema = @Schema(type = "string", example = "https://example.com"))
  private Set<String> logoutRedirectUri;

  /** The scopes assigned to the client. */
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

  /** The date and time when the client was created. */
  @JsonProperty("created_on")
  @Schema(
      description = "When the client was registered, as an ISO-8601 timestamp with a zone offset.",
      example = "2024-04-04T12:00:00Z")
  private ZonedDateTime createdOn;

  /** The user who created the client. */
  @JsonProperty("created_by")
  @Schema(
      description =
          "The identifier of the user who registered the client. A plain user may read and change "
              + "only the clients where this is their own identifier.",
      example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
  private String createdBy;

  /** The date and time when the client was last modified. */
  @JsonProperty("modified_on")
  @Schema(
      description =
          "When the client was last changed, as an ISO-8601 timestamp with a zone offset.",
      example = "2024-04-04T12:00:00Z")
  private ZonedDateTime modifiedOn;

  /** The user who last modified the client. */
  @JsonProperty("modified_by")
  @Schema(
      description = "The identifier of the user who last changed the client.",
      example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
  private String modifiedBy;

  /** Indicates if the client is public. */
  @Getter(onMethod_ = @JsonGetter("is_public"))
  @Schema(
      description =
          "Whether the client is offered to third-party tenants rather than only to the tenant "
              + "that registered it.",
      example = "false")
  private boolean isPublic;

  /** Indicates if the client is enabled. */
  @Schema(
      description =
          "Whether the client may currently obtain tokens. A disabled client keeps its "
              + "registration and the tokens already issued to it, but new authorization requests "
              + "for it are refused.",
      example = "true")
  private boolean enabled;
}
