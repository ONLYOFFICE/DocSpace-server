/**
 *
 */
package com.onlyoffice.authorization.core.entities;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import org.hibernate.annotations.Immutable;

import java.sql.Timestamp;

/**
 *
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
@Immutable
@Entity
@Table(name = "identity_clients")
public class Client {
    @Id
    @Column(name = "client_id")
    private String clientId;
    @Column(name = "client_name")
    private String name;
    private String description;
    @Column(name = "client_secret")
    private String clientSecret;
    @Column(name = "terms_url")
    private String termsUrl;
    @Column(name = "policy_url")
    private String policyUrl;
    @Column(name = "logo_url")
    private String logoUrl;
    @Column(name = "client_issued_at")
    private Timestamp clientIssuedAt;
    @Column(name = "authentication_method")
    private String authenticationMethod;
    @Column(name = "redirect_uri")
    private String redirectUri;
    @Column(name = "logout_redirect_uri")
    private String logoutRedirectUri;
    @Column(name = "scopes")
    private String scopes;
    @Column(name = "tenant_id")
    private int tenant;
    @Column(name = "enabled")
    private Boolean enabled;
    @Column(name = "invalidated")
    private Boolean invalidated;
}