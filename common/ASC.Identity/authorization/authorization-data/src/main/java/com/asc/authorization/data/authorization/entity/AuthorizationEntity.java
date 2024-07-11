// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.authorization.data.authorization.entity;

import jakarta.persistence.*;
import java.io.Serializable;
import java.time.ZonedDateTime;
import lombok.*;
import org.hibernate.annotations.ColumnDefault;

/** Entity class representing an authorization. */
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
  private Integer tenantId;

  /** The state. */
  @Column(name = "state")
  private String state;

  /** The attributes. */
  @Column(name = "attributes")
  private String attributes;

  /** The authorization grant type. */
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

  /** Indicates whether the authorization is invalidated. */
  @Column(name = "is_invalidated")
  @ColumnDefault("false")
  private boolean invalidated;

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
    private static final long serialVersionUID = 1L;
    private String registeredClientId;
    private String principalId;
  }
}
