/**
 *
 */
package com.onlyoffice.authorization.api.core.entities;

import jakarta.persistence.*;
import lombok.*;

import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Objects;

/**
 *
 */
@Data
@Builder
@Entity
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "identity_authorizations")
@IdClass(Authorization.AuthorizationId.class)
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
    private ZonedDateTime authorizationCodeIssuedAt;
    @Column(name = "authorization_code_expires_at")
    private ZonedDateTime authorizationCodeExpiresAt;
    @Column(name = "authorization_code_metadata")
    private String authorizationCodeMetadata;
    @Lob
    @Column(name = "access_token_value", length = 4000)
    private String accessTokenValue;
    @Column(name = "access_token_issued_at")
    private ZonedDateTime accessTokenIssuedAt;
    @Column(name = "access_token_expires_at")
    private ZonedDateTime accessTokenExpiresAt;
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
    private ZonedDateTime refreshTokenIssuedAt;
    @Column(name = "refresh_token_expires_at")
    private ZonedDateTime refreshTokenExpiresAt;
    @Lob
    @Column(name = "refresh_token_metadata", length = 2000)
    private String refreshTokenMetadata;
    @Column(name = "modified_at")
    private ZonedDateTime modifiedAt;
    @Column(name = "invalidated")
    private Boolean invalidated;
    @OneToOne(cascade = CascadeType.REMOVE, fetch = FetchType.LAZY)
    @JoinTable(
            name = "identity_consents",
            joinColumns = {
                    @JoinColumn(name = "principal_name"),
                    @JoinColumn(name = "registered_client_id")
            },
            inverseJoinColumns = {
                    @JoinColumn(name = "registered_client_id"),
                    @JoinColumn(name = "principal_name")
            }
    )
    private Consent consent;
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
            var that = (Authorization.AuthorizationId) o;
            return registeredClientId.equals(that.registeredClientId) && principalName.equals(that.principalName);
        }

        public int hashCode() {
            return Objects.hash(registeredClientId, principalName);
        }
    }
}
