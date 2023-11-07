/**
 *
 */
package com.onlyoffice.authorization.api.core.entities;

import com.fasterxml.jackson.annotation.JsonBackReference;
import jakarta.persistence.*;
import lombok.*;
import org.springframework.data.annotation.CreatedDate;
import org.springframework.data.annotation.LastModifiedDate;

import java.sql.Timestamp;
import java.time.Instant;
import java.util.Set;

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
    @Lob
    private String description;
    @Column(name = "client_secret", unique = true)
    private String clientSecret;
    @Column(name = "logo", columnDefinition = "LONGTEXT")
    private String logo;
    @Column(name = "client_issued_at")
    @CreatedDate
    private Timestamp clientIssuedAt;
    @Column(name = "authentication_method", length = 100)
    private String authenticationMethod;
    @Column(name = "tenant_id")
    private int tenant;
    @Column(name = "tenant_url")
    @Lob
    private String tenantUrl;
    @Column(name = "website_url")
    @Lob
    private String websiteUrl;
    @Column(name = "terms_url")
    @Lob
    private String termsUrl;
    @Column(name = "policy_url")
    @Lob
    private String policyUrl;
    @Column(name = "redirect_uris")
    @Lob
    private String redirectUris;
    @Column(name = "allowed_origins")
    @Lob
    private String allowedOrigins;
    @Column(name = "logout_redirect_uri")
    @Lob
    private String logoutRedirectUri;
    @Column(name = "enabled")
    private boolean enabled;
    @Column(name = "invalidated")
    private boolean invalidated;
    @Column(name = "scopes")
    @Lob
    private String scopes;
    @Column(name = "created_on")
    @LastModifiedDate
    private Timestamp createdOn;
    @Column(name = "created_by")
    private String createdBy;
    @Column(name = "modified_on")
    @LastModifiedDate
    private Timestamp modifiedOn;
    @Column(name = "modified_by")
    private String modifiedBy;
    @OneToMany
    @JoinColumn(name="registered_client_id", referencedColumnName="client_id")
    @JsonBackReference
    private Set<Consent> consents;
    @PrePersist
    private void prePersist() {
        this.enabled = true;
        this.invalidated = false;
        this.createdOn = Timestamp.from(Instant.now());
        this.clientIssuedAt = Timestamp.from(Instant.now());
    }
    @PreUpdate
    private void preUpdate() {
        if (this.invalidated)
            this.enabled = false;
        this.modifiedOn = Timestamp.from(Instant.now());
    }
}