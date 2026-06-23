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
  private String name;

  /** The unique identifier of the client. */
  @JsonProperty("client_id")
  private String clientId;

  /** The client secret. Included only if it is not null. */
  @JsonProperty("client_secret")
  @JsonInclude(JsonInclude.Include.NON_NULL)
  private String clientSecret;

  /** The description of the client. */
  private String description;

  /** The website URL of the client. */
  @JsonProperty("website_url")
  private String websiteUrl;

  /** The terms of service URL of the client. */
  @JsonProperty("terms_url")
  private String termsUrl;

  /** The privacy policy URL of the client. */
  @JsonProperty("policy_url")
  private String policyUrl;

  /** The logo of the client. */
  @JsonProperty("logo")
  private String logo;

  /** The authentication methods supported by the client. */
  @JsonProperty("authentication_methods")
  private Set<String> authenticationMethods;

  /** The tenant ID associated with the client. */
  private long tenant;

  /** The redirect URIs registered for the client. */
  @JsonProperty("redirect_uris")
  private Set<String> redirectUris;

  /** The allowed origins for the client. */
  @JsonProperty("allowed_origins")
  private Set<String> allowedOrigins;

  /** The logout redirect URIs registered for the client. */
  @JsonProperty("logout_redirect_uris")
  private Set<String> logoutRedirectUri;

  /** The scopes assigned to the client. */
  private Set<String> scopes;

  /** The date and time when the client was created. */
  @JsonProperty("created_on")
  private ZonedDateTime createdOn;

  /** The user who created the client. */
  @JsonProperty("created_by")
  private String createdBy;

  /** The date and time when the client was last modified. */
  @JsonProperty("modified_on")
  private ZonedDateTime modifiedOn;

  /** The user who last modified the client. */
  @JsonProperty("modified_by")
  private String modifiedBy;

  /** Indicates if the client is public. */
  @Getter(onMethod_ = @JsonGetter("is_public"))
  private boolean isPublic;

  /** Indicates if the client is enabled. */
  private boolean enabled;
}
