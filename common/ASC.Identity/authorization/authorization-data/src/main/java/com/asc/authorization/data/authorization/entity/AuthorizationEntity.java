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

package com.asc.authorization.data.authorization.entity;

import jakarta.persistence.*;
import java.io.Serial;
import java.io.Serializable;
import java.time.ZonedDateTime;
import lombok.*;

/**
 * Entity class representing an authorization. This class is mapped to the `identity_authorizations`
 * table and manages authorization-related information such as tokens, scopes, and metadata.
 */
@Entity
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "identity_authorizations")
@IdClass(AuthorizationEntity.AuthorizationId.class)
@EqualsAndHashCode
@ToString
public class AuthorizationEntity {
  /** The authorization ID. */
  @Column(name = "id", nullable = false)
  private String id;

  /** The registered client ID. */
  @Id
  @Column(name = "registered_client_id", nullable = false)
  private String registeredClientId;

  /** The principal ID. */
  @Id
  @Column(name = "principal_id", nullable = false)
  private String principalId;

  /** The tenant ID. */
  @Column(name = "tenant_id", nullable = false)
  private long tenantId;

  /** The state. */
  @Column(name = "state")
  private String state;

  /** The attributes. */
  @Column(name = "attributes")
  private String attributes;

  /** The authorization grant type. */
  @Id
  @Column(name = "authorization_grant_type", nullable = false)
  private String authorizationGrantType;

  /** The authorized scopes. */
  @Column(name = "authorized_scopes")
  private String authorizedScopes;

  /** The authorization code value. */
  @Column(name = "authorization_code_value")
  private String authorizationCodeValue;

  /** The authorization code metadata. */
  @Column(name = "authorization_code_metadata")
  private String authorizationCodeMetadata;

  /** The authorization code issued at timestamp. */
  @Column(name = "authorization_code_issued_at")
  private ZonedDateTime authorizationCodeIssuedAt;

  /** The authorization code expires at timestamp. */
  @Column(name = "authorization_code_expires_at")
  private ZonedDateTime authorizationCodeExpiresAt;

  /** The access token type. */
  @Column(name = "access_token_type")
  private String accessTokenType;

  /** The access token value. */
  @Column(name = "access_token_value")
  private String accessTokenValue;

  /** The access token hash. */
  @Column(name = "access_token_hash")
  private String accessTokenHash;

  /** The access token scopes. */
  @Column(name = "access_token_scopes")
  private String accessTokenScopes;

  /** The access token metadata. */
  @Column(name = "access_token_metadata")
  private String accessTokenMetadata;

  /** The access token issued at timestamp. */
  @Column(name = "access_token_issued_at")
  private ZonedDateTime accessTokenIssuedAt;

  /** The access token expires at timestamp. */
  @Column(name = "access_token_expires_at")
  private ZonedDateTime accessTokenExpiresAt;

  /** The refresh token value. */
  @Column(name = "refresh_token_value")
  private String refreshTokenValue;

  /** The refresh token hash. */
  @Column(name = "refresh_token_hash")
  private String refreshTokenHash;

  /** The refresh token metadata. */
  @Column(name = "refresh_token_metadata")
  private String refreshTokenMetadata;

  /** The refresh token issued at timestamp. */
  @Column(name = "refresh_token_issued_at")
  private ZonedDateTime refreshTokenIssuedAt;

  /** The refresh token expires at timestamp. */
  @Column(name = "refresh_token_expires_at")
  private ZonedDateTime refreshTokenExpiresAt;

  /** The ID token value issued as part of the authorization for openid scope. */
  @Column(name = "id_token_value")
  private String idTokenValue;

  /** The claims associated with the ID token, typically represented in JSON format. */
  @Column(name = "id_token_claims")
  private String idTokenClaims;

  /** Metadata associated with the ID token. */
  @Column(name = "id_token_metadata")
  private String idTokenMetadata;

  /** The timestamp when the ID token was issued. */
  @Column(name = "id_token_issued_at")
  private ZonedDateTime idTokenIssuedAt;

  /** The timestamp when the ID token is set to expire. */
  @Column(name = "id_token_expires_at")
  private ZonedDateTime idTokenExpiresAt;

  /** The modified at timestamp. */
  @Column(name = "modified_at")
  private ZonedDateTime modifiedAt;

  /**
   * This method is called before updating the entity and sets the modified_at field to the current
   * date and time.
   */
  @PreUpdate
  private void preUpdate() {
    this.modifiedAt = ZonedDateTime.now();
  }

  /** The composite primary key class for the AuthorizationEntity. */
  @Data
  @AllArgsConstructor
  @NoArgsConstructor
  public static class AuthorizationId implements Serializable {
    @Serial private static final long serialVersionUID = 1L;
    private String registeredClientId;
    private String principalId;
    private String authorizationGrantType;
  }
}
