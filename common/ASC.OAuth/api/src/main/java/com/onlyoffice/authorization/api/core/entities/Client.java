/**
 *
 */
package com.onlyoffice.authorization.api.core.entities;

import jakarta.persistence.*;
import lombok.*;
import org.springframework.data.annotation.CreatedDate;

import java.sql.Timestamp;
import java.time.Instant;

/**
 *
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Entity
@Table(name = "identity_clients")
@ToString
public class Client {
    @Id
    @Column(name = "client_id", unique = true, length = 36)
    private String clientId;
    @Column(name = "client_name")
    private String name;
    private String description;
    @Column(name = "client_secret", unique = true, length = 36)
    private String clientSecret;
    @Column(name = "terms_url")
    @Lob
    private String termsUrl;
    @Column(name = "policy_url")
    @Lob
    private String policyUrl;
    @Column(name = "logo_url")
    @Lob
    private String logoUrl;
    @Column(name = "client_issued_at")
    @CreatedDate
    private Timestamp clientIssuedAt;
    @Column(name = "authentication_method", length = 100)
    private String authenticationMethod;
    @Column(name = "redirect_uri")
    @Lob
    private String redirectUri;
    @Column(name = "logout_redirect_uri")
    @Lob
    private String logoutRedirectUri;
    @Column(name = "scopes")
    @Lob
    private String scopes;
    @Column(name = "tenant_id")
    private int tenant;
    @Column(name = "enabled")
    private Boolean enabled;
    @Column(name = "invalidated")
    private Boolean invalidated;
    @PrePersist
    private void prePersist() {
        this.enabled = true;
        this.invalidated = false;
        this.clientIssuedAt = Timestamp.from(Instant.now());
    }
    @PreUpdate
    private void preUpdate() {
        if (this.invalidated)
            this.enabled = false;
    }
}