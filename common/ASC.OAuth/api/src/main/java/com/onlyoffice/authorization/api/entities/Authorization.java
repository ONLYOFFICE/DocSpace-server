package com.onlyoffice.authorization.api.entities;

import jakarta.persistence.*;
import lombok.*;

import java.io.Serializable;
import java.sql.Timestamp;
import java.util.Date;
import java.util.Objects;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Entity
@IdClass(Authorization.AuthorizationId.class)
@Table(name = "identity_authorizations")
public class Authorization {
    @Column(unique = true)
    private String id;
    @Id
    @Column(name = "registered_client_id")
    private String registeredClientId;
    @Id
    @Column(name = "principal_name")
    private String principalName;
    @Column(name = "authorization_grant_type")
    private String authorizationGrantType;
    @Column(name = "authorized_scopes", length = 1000)
    private String authorizedScopes;
    @Lob
    @Column(name = "attributes", length = 4000)
    private String attributes;
    @Column(name = "state", length = 500)
    private String state;
    @Lob
    @Column(name = "authorization_code_value", length = 4000)
    private String authorizationCodeValue;
    @Column(name = "authorization_code_issued_at")
    private Date authorizationCodeIssuedAt;
    @Column(name = "authorization_code_expires_at")
    private Date authorizationCodeExpiresAt;
    @Column(name = "authorization_code_metadata")
    private String authorizationCodeMetadata;
    @Lob
    @Column(name = "access_token_value", length = 4000)
    private String accessTokenValue;
    @Column(name = "access_token_issued_at")
    private Date accessTokenIssuedAt;
    @Column(name = "access_token_expires_at")
    private Date accessTokenExpiresAt;
    @Lob
    @Column(name = "access_token_metadata", length = 2000)
    private String accessTokenMetadata;
    @Column(name = "access_token_type")
    private String accessTokenType;
    @Column(name = "access_token_scopes", length = 1000)
    private String accessTokenScopes;
    @Lob
    @Column(name = "refresh_token_value", length = 4000)
    private String refreshTokenValue;
    @Column(name = "refresh_token_issued_at")
    private Date refreshTokenIssuedAt;
    @Column(name = "refresh_token_expires_at")
    private Date refreshTokenExpiresAt;
    @Lob
    @Column(name = "refresh_token_metadata", length = 2000)
    private String refreshTokenMetadata;
    @Column(name = "modified_at")
    private Timestamp modifiedAt;
    @Column(name = "invalidated")
    private Boolean invalidated;
    @PrePersist
    private void prePersist() {
        this.invalidated = false;
    }
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
