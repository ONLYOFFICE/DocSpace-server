// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.data.client.entity;

import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.registration.data.client.converter.AuthenticationMethodConverter;
import com.asc.registration.data.scope.entity.ScopeEntity;
import jakarta.persistence.*;
import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Set;
import lombok.*;
import org.hibernate.annotations.BatchSize;
import org.hibernate.annotations.ColumnDefault;
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
public class ClientEntity implements Serializable {

  /** The unique identifier for the client. */
  @Id
  @Column(name = "client_id", unique = true, length = 36)
  private String clientId;

  /** The identifier for the tenant associated with the client. */
  @Column(name = "tenant_id", nullable = false)
  private long tenantId;

  /** The secret for the client. This field is unique. */
  @Column(name = "client_secret", unique = true, nullable = false)
  private String clientSecret;

  /** The name of the client. */
  private String name;

  /** The description of the client. */
  @Lob private String description;

  /** The logo for the client. */
  @Column(name = "logo", columnDefinition = "LONGTEXT")
  private String logo;

  @ElementCollection(fetch = FetchType.LAZY, targetClass = AuthenticationMethod.class)
  @CollectionTable(
      name = "identity_client_authentication_methods",
      joinColumns = @JoinColumn(name = "client_id"))
  @Convert(converter = AuthenticationMethodConverter.class)
  @Column(name = "authentication_method", length = 100)
  @BatchSize(size = 10)
  private Set<AuthenticationMethod> authenticationMethods;

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
  @ElementCollection(fetch = FetchType.LAZY)
  @CollectionTable(
      name = "identity_client_redirect_uris",
      joinColumns = @JoinColumn(name = "client_id"))
  @Column(name = "redirect_uri")
  @BatchSize(size = 10)
  private Set<String> redirectUris;

  /** The allowed origins for the client. */
  @ElementCollection(fetch = FetchType.LAZY)
  @CollectionTable(
      name = "identity_client_allowed_origins",
      joinColumns = @JoinColumn(name = "client_id"))
  @Column(name = "allowed_origin")
  @BatchSize(size = 10)
  private Set<String> allowedOrigins;

  /** The logout redirect URI for the client. */
  @Lob
  @Column(name = "logout_redirect_uri")
  private String logoutRedirectUri;

  /** Indicates whether the client is public. */
  @Column(name = "is_public")
  @ColumnDefault("false")
  private boolean accessible;

  /** Indicates whether the client is enabled. */
  @Column(name = "is_enabled")
  @ColumnDefault("true")
  private boolean enabled;

  /** Indicates whether the client is invalidated. */
  @Column(name = "is_invalidated")
  @ColumnDefault("false")
  private boolean invalidated;

  /** The scopes for the client. */
  @ManyToMany(fetch = FetchType.LAZY)
  @JoinTable(
      name = "identity_client_scopes",
      joinColumns = @JoinColumn(name = "client_id"),
      inverseJoinColumns = @JoinColumn(name = "scope_name"))
  @BatchSize(size = 10)
  private Set<ScopeEntity> scopes;

  /** The date and time the client was created. */
  @CreatedDate
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

  /**
   * The version field is used for optimistic locking. It tracks the number of updates made to the
   * entity. This helps to avoid conflicts during concurrent updates by ensuring that a transaction
   * updates an entity based on the most recent version.
   */
  @Version private Integer version;

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
