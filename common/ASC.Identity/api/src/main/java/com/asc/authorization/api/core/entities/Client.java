/**
 *
 */
package com.asc.authorization.api.core.entities;

import com.fasterxml.jackson.annotation.JsonBackReference;
import jakarta.persistence.*;
import lombok.*;
import org.springframework.data.annotation.CreatedDate;
import org.springframework.data.annotation.LastModifiedDate;

import java.time.ZonedDateTime;
import java.util.Set;

/**
 *
 */
@Entity
@Getter
@Setter
@Builder
@ToString
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "identity_clients")
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
    @CreatedDate
    @Column(name = "client_issued_at")
    private ZonedDateTime clientIssuedAt;
    @Column(name = "authentication_method", length = 100)
    private String authenticationMethod;
    @Column(name = "tenant_id")
    private int tenant;
    @Lob
    @Column(name = "tenant_url")
    private String tenantUrl;
    @Lob
    @Column(name = "website_url")
    private String websiteUrl;
    @Lob
    @Column(name = "terms_url")
    private String termsUrl;
    @Lob
    @Column(name = "policy_url")
    private String policyUrl;
    @Lob
    @Column(name = "redirect_uris")
    private String redirectUris;
    @Lob
    @Column(name = "allowed_origins")
    private String allowedOrigins;
    @Lob
    @Column(name = "logout_redirect_uri")
    private String logoutRedirectUri;
    @Column(name = "enabled")
    private boolean enabled;
    @Column(name = "invalidated")
    private boolean invalidated;
    @Lob
    @Column(name = "scopes")
    private String scopes;
    @LastModifiedDate
    @Column(name = "created_on")
    private ZonedDateTime createdOn;
    @Column(name = "created_by")
    private String createdBy;
    @LastModifiedDate
    @Column(name = "modified_on")
    private ZonedDateTime modifiedOn;
    @Column(name = "modified_by")
    private String modifiedBy;
    @JsonBackReference
    @OneToMany(mappedBy = "client")
    private Set<Consent> consents;
    @PrePersist
    private void prePersist() {
        this.enabled = true;
        this.invalidated = false;
        this.createdOn = ZonedDateTime.now();
        this.clientIssuedAt = ZonedDateTime.now();
    }
    @PreUpdate
    private void preUpdate() {
        if (this.invalidated)
            this.enabled = false;
    }
}