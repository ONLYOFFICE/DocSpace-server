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

package com.asc.registration.service.transfer.request.create;

import com.asc.common.utilities.validation.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.*;
import java.io.Serializable;
import java.util.Set;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

/**
 * CreateTenantClientCommand is a Data Transfer Object (DTO) used to transfer data for creating a
 * tenant client. It contains validation annotations to ensure data integrity.
 */
@Getter
@Setter
@Builder
@AllArgsConstructor
public class CreateTenantClientCommand implements Serializable {

  /** The ID of the tenant. Must be greater than or equal to 1. */
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private long tenantId;

  /** The name of the client. Must not be empty and should be between 3 and 256 characters. */
  @NotBlank(message = "client name must not be empty")
  @Size(
      min = 3,
      max = 256,
      message = "client name length is expected to be between 3 and 256 characters")
  private String name;

  /** A description of the client. */
  private String description;

  /** The logo of the client, expected to be passed as a base64 string. */
  @Pattern(
      regexp = "^data:image\\/(?:png|jpeg|jpg|svg\\+xml);base64,.*.{1,}",
      message = "client logo is expected to be passed as base64")
  private String logo;

  /** Indicates if PKCE (Proof Key for Code Exchange) is allowed. */
  @JsonProperty("allow_pkce")
  private boolean allowPkce;

  /** Indicates if the client is public. */
  @JsonProperty("is_public")
  private boolean isPublic;

  /** The website URL of the client. Must be a valid URL. */
  @JsonProperty("website_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "website url is expected to be passed as a valid url")
  private String websiteUrl;

  /** The terms of service URL of the client. Must be a valid URL. */
  @JsonProperty("terms_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "terms url is expected to be passed as a valid url")
  private String termsUrl;

  /** The privacy policy URL of the client. Must be a valid URL. */
  @JsonProperty("policy_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "policy url is expected to be passed as a valid url")
  private String policyUrl;

  /** The redirect URIs for the client. Each must be a valid URL. */
  @JsonProperty("redirect_uris")
  @URLCollection
  @Size(
      min = 1,
      max = 12,
      message = "redirect uris must contain at least 1 and at most 12 addresses")
  private Set<String> redirectUris;

  /** The allowed origins for the client. Each must be a valid URL. */
  @JsonProperty("allowed_origins")
  @URLCollection
  @Size(
      min = 1,
      max = 12,
      message = "allowed origins must contain at least 1 and at most 12 addresses")
  private Set<String> allowedOrigins;

  /** The logout redirect URI for the client. Must be a valid URL. */
  @JsonProperty("logout_redirect_uri")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "logout redirect uri is expected to be passed as a valid url")
  private String logoutRedirectUri;

  /** The scopes for the client. Must not be empty. */
  @NotEmpty(message = "scopes field cannot be empty")
  private Set<String> scopes;
}
