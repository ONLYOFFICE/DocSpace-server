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

package com.asc.common.service.transfer.message;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.ZonedDateTime;
import lombok.*;

/**
 * Message object used for saving authorization information to a remote region via messaging (RPC).
 *
 * <p>This DTO is used to request authorization persistence in a remote region when the
 * authorization code's region prefix doesn't match the current region.
 */
@Builder
@Getter
@Setter
@ToString
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class SaveAuthorizationMessage {
  @JsonProperty(value = "id")
  private String id;

  @JsonProperty(value = "registered_client_id", required = true)
  private String registeredClientId;

  @JsonProperty(value = "principal_id", required = true)
  private String principalId;

  @JsonProperty(value = "tenant_id")
  private Long tenantId;

  @JsonProperty(value = "state")
  private String state;

  @JsonProperty(value = "attributes")
  private String attributes;

  @JsonProperty(value = "authorization_grant_type", required = true)
  private String authorizationGrantType;

  @JsonProperty(value = "authorized_scopes")
  private String authorizedScopes;

  @JsonProperty(value = "authorization_code_value")
  private String authorizationCodeValue;

  @JsonProperty(value = "authorization_code_metadata")
  private String authorizationCodeMetadata;

  @JsonProperty(value = "authorization_code_issued_at")
  private ZonedDateTime authorizationCodeIssuedAt;

  @JsonProperty(value = "authorization_code_expires_at")
  private ZonedDateTime authorizationCodeExpiresAt;

  @JsonProperty(value = "access_token_type")
  private String accessTokenType;

  @JsonProperty(value = "access_token_value")
  private String accessTokenValue;

  @JsonProperty(value = "access_token_hash")
  private String accessTokenHash;

  @JsonProperty(value = "access_token_scopes")
  private String accessTokenScopes;

  @JsonProperty(value = "access_token_metadata")
  private String accessTokenMetadata;

  @JsonProperty(value = "access_token_issued_at")
  private ZonedDateTime accessTokenIssuedAt;

  @JsonProperty(value = "access_token_expires_at")
  private ZonedDateTime accessTokenExpiresAt;

  @JsonProperty(value = "refresh_token_value")
  private String refreshTokenValue;

  @JsonProperty(value = "refresh_token_hash")
  private String refreshTokenHash;

  @JsonProperty(value = "refresh_token_metadata")
  private String refreshTokenMetadata;

  @JsonProperty(value = "refresh_token_issued_at")
  private ZonedDateTime refreshTokenIssuedAt;

  @JsonProperty(value = "refresh_token_expires_at")
  private ZonedDateTime refreshTokenExpiresAt;

  @JsonProperty(value = "id_token_value")
  private String idTokenValue;

  @JsonProperty(value = "id_token_claims")
  private String idTokenClaims;

  @JsonProperty(value = "id_token_metadata")
  private String idTokenMetadata;

  @JsonProperty(value = "id_token_issued_at")
  private ZonedDateTime idTokenIssuedAt;

  @JsonProperty(value = "id_token_expires_at")
  private ZonedDateTime idTokenExpiresAt;
}
