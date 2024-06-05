package com.asc.common.data.client.entity;

import com.asc.common.data.consent.entity.ConsentEntity;
import com.fasterxml.jackson.annotation.JsonBackReference;
import jakarta.persistence.*;
import java.time.ZonedDateTime;
import java.util.Set;
import lombok.*;
import org.springframework.data.annotation.CreatedDate;
import org.springframework.data.annotation.LastModifiedDate;

/** Represents a client entity. */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Entity
@Table(name = "identity_clients")
public class ClientEntity {

  /** The unique identifier for the client. */
  @Id
  @Column(name = "client_id", unique = true, length = 36)
  private String clientId;

  /** The name of the client. */
  @Column(name = "client_name")
  private String name;

  /** The description of the client. */
  @Lob private String description;

  /** The secret for the client. This field is unique. */
  @Column(name = "client_secret", unique = true)
  private String clientSecret;

  /** The logo for the client. */
  @Column(name = "logo", columnDefinition = "LONGTEXT")
  private String logo;

  /** The date and time the client was issued. */
  @CreatedDate
  @Column(name = "client_issued_at")
  private ZonedDateTime clientIssuedAt;

  /** The authentication method for the client. */
  @Column(name = "authentication_method", length = 100)
  private String authenticationMethod;

  /** The identifier for the tenant associated with the client. */
  @Column(name = "tenant_id")
  private int tenant;

  /** The URL for the tenant associated with the client. */
  @Lob
  @Column(name = "tenant_url")
  private String tenantUrl;

  /** The URL for the client's website. */
  @Lob
  @Column(name = "website_url")
  private String websiteUrl;

  /** The URL for the client's terms. */
  @Lob
  @Column(name = "terms_url")
  private String termsUrl;

  /** The URL for the client's policy. */
  @Lob
  @Column(name = "policy_url")
  private String policyUrl;

  /** The redirect URIs for the client. */
  @Lob
  @Column(name = "redirect_uris")
  private String redirectUris;

  /** The allowed origins for the client. */
  @Lob
  @Column(name = "allowed_origins")
  private String allowedOrigins;

  /** The logout redirect URI for the client. */
  @Lob
  @Column(name = "logout_redirect_uri")
  private String logoutRedirectUri;

  /** Indicates whether the client is enabled. */
  @Column(name = "enabled")
  private boolean enabled;

  /** Indicates whether the client is invalidated. */
  @Column(name = "invalidated")
  private boolean invalidated;

  /** The scopes for the client. */
  @Lob
  @Column(name = "scopes")
  private String scopes;

  /** The date and time the client was created. */
  @LastModifiedDate
  @Column(name = "created_on")
  private ZonedDateTime createdOn;

  /** The user who created the client. */
  @Column(name = "created_by")
  private String createdBy;

  /** The date and time the client was last modified. */
  @LastModifiedDate
  @Column(name = "modified_on")
  private ZonedDateTime modifiedOn;

  /** The user who last modified the client. */
  @Column(name = "modified_by")
  private String modifiedBy;

  /** The consents associated with the client. */
  @JsonBackReference
  @OneToMany(mappedBy = "client")
  private Set<ConsentEntity> consents;

  /**
   * This method is called before the entity is persisted. It sets the enabled flag to true, the
   * invalidated flag to false, and sets the createdOn and clientIssuedAt fields to the current date
   * and time.
   */
  @PrePersist
  private void prePersist() {
    this.enabled = true;
    this.invalidated = false;
    this.createdOn = ZonedDateTime.now();
    this.clientIssuedAt = ZonedDateTime.now();
  }

  /**
   * This method is called before the entity is updated. It sets the enabled flag to false if the
   * invalidated flag is true.
   */
  @PreUpdate
  private void preUpdate() {
    if (this.invalidated) this.enabled = false;
  }
}
