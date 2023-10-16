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

import java.time.Instant;

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
    private String clientName;
    @Column(name = "client_secret")
    private String clientSecret;
    @Column(name = "client_issued_at")
    private Instant clientIssuedAt;
    @Column(name = "authentication_method")
    private String clientAuthenticationMethod;
    @Column(name = "redirect_uri")
    private String redirectUri;
    @Column(name = "logout_redirect_uri")
    private String postLogoutRedirectUri;
    @Column(name = "scopes")
    private String scopes;
    @Column(name = "tenant_id")
    private Integer tenantId;
    @Column(name = "invalidated")
    private Boolean invalidated;
}