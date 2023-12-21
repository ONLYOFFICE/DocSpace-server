/**
 *
 */
package com.onlyoffice.authorization.core.entities;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.Immutable;

import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Objects;

/**
 *
 */
@Getter
@Setter
@Entity
@Builder
@ToString
@Immutable
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "identity_authorizations")
@IdClass(Authorization.AuthorizationId.class)
public class Authorization {
    @Column
    private String id;
    @Id
    @Column(name = "registered_client_id")
    private String registeredClientId;
    @Id
    @Column(name = "principal_name")
    private String principalName;
    @Column(name = "authorization_grant_type")
    private String authorizationGrantType;
    @Column(name = "authorized_scopes")
    private String authorizedScopes;
    @Column(name = "attributes")
    private String attributes;
    @Column(name = "state")
    private String state;
    @Column(name = "authorization_code_value")
    private String authorizationCodeValue;
    @Column(name = "authorization_code_issued_at")
    private ZonedDateTime authorizationCodeIssuedAt;
    @Column(name = "authorization_code_expires_at")
    private ZonedDateTime authorizationCodeExpiresAt;
    @Column(name = "authorization_code_metadata")
    private String authorizationCodeMetadata;
    @Column(name = "access_token_value")
    private String accessTokenValue;
    @Column(name = "access_token_issued_at")
    private ZonedDateTime accessTokenIssuedAt;
    @Column(name = "access_token_expires_at")
    private ZonedDateTime accessTokenExpiresAt;
    @Column(name = "access_token_metadata")
    private String accessTokenMetadata;
    @Column(name = "access_token_type")
    private String accessTokenType;
    @Column(name = "access_token_scopes")
    private String accessTokenScopes;
    @Column(name = "refresh_token_value")
    private String refreshTokenValue;
    @Column(name = "refresh_token_issued_at")
    private ZonedDateTime refreshTokenIssuedAt;
    @Column(name = "refresh_token_expires_at")
    private ZonedDateTime refreshTokenExpiresAt;
    @Column(name = "refresh_token_metadata")
    private String refreshTokenMetadata;
    @Column(name = "modified_at")
    private ZonedDateTime modifiedAt;
    @Column(name = "invalidated")
    private Boolean invalidated;
    @Getter
    @Setter
    @AllArgsConstructor
    @NoArgsConstructor
    public static class AuthorizationId implements Serializable {
        private static final long serialVersionUID = 1L;
        private String registeredClientId;
        private String principalName;

        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;
            Authorization.AuthorizationId that = (Authorization.AuthorizationId) o;
            return registeredClientId.equals(that.registeredClientId) && principalName.equals(that.principalName);
        }

        public int hashCode() {
            return Objects.hash(registeredClientId, principalName);
        }
    }
}