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

import java.time.ZonedDateTime;
import java.util.Set;
import lombok.Setter;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.*;

/**
 * Represents a DynamoDB entity for storing client information.
 *
 * <p>This entity maps client-related attributes to DynamoDB attributes and includes annotations for
 * primary keys, secondary keys, and other properties. It supports storing and retrieving client
 * details such as authentication methods, URLs, and tenant-specific information.
 */
@Setter
@DynamoDbBean
public class ClientDynamoEntity {
  private String clientId;
  private long tenantId;
  private String clientSecret;
  private String name;
  private String description;
  private String logo;
  private Set<String> authenticationMethods;
  private String websiteUrl;
  private String termsUrl;
  private String policyUrl;
  private Set<String> redirectUris;
  private Set<String> allowedOrigins;
  private String logoutRedirectUri;
  private boolean accessible;
  private boolean enabled;
  private Set<String> scopes;
  private ZonedDateTime createdOn;
  private String createdBy;
  private ZonedDateTime modifiedOn;
  private String modifiedBy;

  /**
   * Gets the client ID, which serves as the partition key in DynamoDB.
   *
   * @return the client ID.
   */
  @DynamoDbPartitionKey
  @DynamoDbAttribute("client_id")
  public String getClientId() {
    return clientId;
  }

  /**
   * Gets the tenant ID, which is also a secondary partition key in the "tenant-created-index".
   *
   * @return the tenant ID.
   */
  @DynamoDbAttribute("tenant_id")
  @DynamoDbSecondaryPartitionKey(indexNames = "tenant-created-index")
  public long getTenantId() {
    return tenantId;
  }

  /**
   * Gets the client secret.
   *
   * @return the client secret.
   */
  @DynamoDbAttribute("client_secret")
  public String getClientSecret() {
    return clientSecret;
  }

  /**
   * Gets the name of the client.
   *
   * @return the client name.
   */
  @DynamoDbAttribute("name")
  public String getName() {
    return name;
  }

  /**
   * Gets the description of the client.
   *
   * @return the client description.
   */
  @DynamoDbAttribute("description")
  public String getDescription() {
    return description;
  }

  /**
   * Gets the logo URL of the client.
   *
   * @return the client logo URL.
   */
  @DynamoDbAttribute("logo")
  public String getLogo() {
    return logo;
  }

  /**
   * Gets the authentication methods supported by the client.
   *
   * @return a set of authentication methods.
   */
  @DynamoDbAttribute("authentication_methods")
  public Set<String> getAuthenticationMethods() {
    return authenticationMethods;
  }

  /**
   * Gets the website URL of the client.
   *
   * @return the website URL.
   */
  @DynamoDbAttribute("website_url")
  public String getWebsiteUrl() {
    return websiteUrl;
  }

  /**
   * Gets the terms of service URL for the client.
   *
   * @return the terms URL.
   */
  @DynamoDbAttribute("terms_url")
  public String getTermsUrl() {
    return termsUrl;
  }

  /**
   * Gets the privacy policy URL for the client.
   *
   * @return the policy URL.
   */
  @DynamoDbAttribute("policy_url")
  public String getPolicyUrl() {
    return policyUrl;
  }

  /**
   * Gets the redirect URIs allowed for the client.
   *
   * @return a set of redirect URIs.
   */
  @DynamoDbAttribute("redirect_uris")
  public Set<String> getRedirectUris() {
    return redirectUris;
  }

  /**
   * Gets the allowed origins for the client.
   *
   * @return a set of allowed origins.
   */
  @DynamoDbAttribute("allowed_origins")
  public Set<String> getAllowedOrigins() {
    return allowedOrigins;
  }

  /**
   * Gets the logout redirect URI for the client.
   *
   * @return the logout redirect URI.
   */
  @DynamoDbAttribute("logout_redirect_uri")
  public String getLogoutRedirectUri() {
    return logoutRedirectUri;
  }

  /**
   * Checks if the client is accessible.
   *
   * @return {@code true} if the client is accessible, {@code false} otherwise.
   */
  @DynamoDbAttribute("accessible")
  public boolean isAccessible() {
    return accessible;
  }

  /**
   * Checks if the client is enabled.
   *
   * @return {@code true} if the client is enabled, {@code false} otherwise.
   */
  @DynamoDbAttribute("enabled")
  public boolean isEnabled() {
    return enabled;
  }

  /**
   * Gets the scopes associated with the client.
   *
   * @return a set of scopes.
   */
  @DynamoDbAttribute("scopes")
  public Set<String> getScopes() {
    return scopes;
  }

  /**
   * Gets the creation timestamp of the client, formatted as a string.
   *
   * @return the creation timestamp.
   */
  @DynamoDbAttribute("created_on")
  @DynamoDbSecondarySortKey(indexNames = "tenant-created-index")
  public String getCreatedOn() {
    return createdOn != null ? createdOn.toString() : null;
  }

  /**
   * Sets the creation timestamp for the client.
   *
   * @param createdOn the creation timestamp as a string.
   */
  public void setCreatedOn(String createdOn) {
    this.createdOn = createdOn != null ? ZonedDateTime.parse(createdOn) : null;
  }

  /**
   * Gets the identifier of the user who created the client.
   *
   * @return the creator's identifier.
   */
  @DynamoDbAttribute("created_by")
  public String getCreatedBy() {
    return createdBy;
  }

  /**
   * Gets the last modified timestamp of the client, formatted as a string.
   *
   * @return the last modified timestamp.
   */
  @DynamoDbAttribute("modified_on")
  public String getModifiedOn() {
    return modifiedOn != null ? modifiedOn.toString() : null;
  }

  /**
   * Sets the last modified timestamp for the client.
   *
   * @param modifiedOn the last modified timestamp as a string.
   */
  public void setModifiedOn(String modifiedOn) {
    this.modifiedOn = modifiedOn != null ? ZonedDateTime.parse(modifiedOn) : null;
  }

  /**
   * Gets the identifier of the user who last modified the client.
   *
   * @return the modifier's identifier.
   */
  @DynamoDbAttribute("modified_by")
  public String getModifiedBy() {
    return modifiedBy;
  }
}
