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
  /** The registered client ID. */
  @Id
  @Column(name = "registered_client_id", nullable = false)
  private String registeredClientId;

  /** The principal name. */
  @Id
  @Column(name = "principal_name", nullable = false)
  private String principalName;

  /** The authorization ID. */
  @Column(name = "id", nullable = false)
  private String id;

  /** The authorization grant type. */
  @Column(name = "authorization_grant_type", nullable = false)
  private String authorizationGrantType;

  /** The authorized scopes. */
  @Column(name = "authorized_scopes")
  private String authorizedScopes;

  /** The attributes. */
  @Column(name = "attributes")
  private String attributes;

  /** The state. */
  @Column(name = "state")
  private String state;

  /** The authorization code value. */
  @Column(name = "authorization_code_value")
  private String authorizationCodeValue;

  /** The authorization code issued at timestamp. */
  @Column(name = "authorization_code_issued_at")
  private ZonedDateTime authorizationCodeIssuedAt;

  /** The authorization code expires at timestamp. */
  @Column(name = "authorization_code_expires_at")
  private ZonedDateTime authorizationCodeExpiresAt;

  /** The authorization code metadata. */
  @Column(name = "authorization_code_metadata")
  private String authorizationCodeMetadata;

  /** The access token value. */
  @Column(name = "access_token_value")
  private String accessTokenValue;

  /** The access token issued at timestamp. */
  @Column(name = "access_token_issued_at")
  private ZonedDateTime accessTokenIssuedAt;

  /** The access token expires at timestamp. */
  @Column(name = "access_token_expires_at")
  private ZonedDateTime accessTokenExpiresAt;

  /** The access token metadata. */
  @Column(name = "access_token_metadata")
  private String accessTokenMetadata;

  /** The access token type. */
  @Column(name = "access_token_type")
  private String accessTokenType;

  /** The access token scopes. */
  @Column(name = "access_token_scopes")
  private String accessTokenScopes;

  /** The refresh token value. */
  @Column(name = "refresh_token_value")
  private String refreshTokenValue;

  /** The refresh token issued at timestamp. */
  @Column(name = "refresh_token_issued_at")
  private ZonedDateTime refreshTokenIssuedAt;

  /** The refresh token expires at timestamp. */
  @Column(name = "refresh_token_expires_at")
  private ZonedDateTime refreshTokenExpiresAt;

  /** The refresh token metadata. */
  @Column(name = "refresh_token_metadata")
  private String refreshTokenMetadata;

  /** The modified at timestamp. */
  @Column(name = "modified_at")
  private ZonedDateTime modifiedAt;

  /** Indicates whether the authorization is invalidated. */
  @Column(name = "invalidated")
  @ColumnDefault("false")
  private Boolean invalidated;

  /** The composite primary key class for the AuthorizationEntity. */
  @Data
  @AllArgsConstructor
  @NoArgsConstructor
  public static class AuthorizationId implements Serializable {
    private static final long serialVersionUID = 1L;
    private String registeredClientId;
    private String principalName;
  }
}
