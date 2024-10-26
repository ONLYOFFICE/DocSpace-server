// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.registration.core.domain.entity;

import com.asc.common.core.domain.entity.AggregateRoot;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.exception.ClientDomainException;
import com.asc.registration.core.domain.value.*;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Collections;
import java.util.HashSet;
import java.util.Set;
import java.util.UUID;
import java.util.function.Function;

/**
 * Represents the client aggregate root in the domain-driven design context. This class encapsulates
 * client-specific information and behaviors, managing the lifecycle and state of a client.
 */
public class Client extends AggregateRoot<ClientId> {
  private static final String UTC = "UTC";
  private ClientId id;
  private ClientSecret secret;
  private final Set<AuthenticationMethod> authenticationMethods;
  private final Set<String> scopes;
  private ClientInfo clientInfo;
  private final ClientTenantInfo clientTenantInfo;
  private ClientWebsiteInfo clientWebsiteInfo;
  private ClientRedirectInfo clientRedirectInfo;
  private ClientCreationInfo clientCreationInfo;
  private ClientModificationInfo clientModificationInfo;
  private ClientStatus clientStatus;
  private ClientVisibility clientVisibility;
  private int version;

  private Client(Builder builder) {
    super.setId(builder.id);
    this.id = builder.id;
    this.secret = builder.secret;
    this.authenticationMethods =
        builder.authenticationMethods != null
            ? new HashSet<>(builder.authenticationMethods)
            : new HashSet<>();
    if (this.authenticationMethods.isEmpty())
      this.authenticationMethods.add(AuthenticationMethod.DEFAULT_AUTHENTICATION);
    this.scopes = builder.scopes != null ? new HashSet<>(builder.scopes) : new HashSet<>();
    this.clientInfo = builder.clientInfo;
    this.clientTenantInfo = builder.clientTenantInfo;
    this.clientWebsiteInfo = builder.clientWebsiteInfo;
    this.clientRedirectInfo = builder.clientRedirectInfo;
    this.clientCreationInfo = builder.clientCreationInfo;
    this.clientModificationInfo = builder.clientModificationInfo;
    this.clientStatus = builder.clientStatus;
    this.clientVisibility =
        builder.clientVisibility == null ? ClientVisibility.PRIVATE : builder.clientVisibility;
    this.version = builder.version;
  }

  /**
   * Initializes the client with the specified creator.
   *
   * @param createdBy the identifier of the creator
   * @throws ClientDomainException if the client has already been initialized
   */
  public void initialize(String createdBy) {
    if (clientStatus != null)
      throw new ClientDomainException("Client has already been initialized");
    this.id = new ClientId(UUID.randomUUID());
    this.secret = new ClientSecret(UUID.randomUUID().toString());
    this.clientCreationInfo =
        ClientCreationInfo.Builder.builder()
            .createdBy(createdBy)
            .createdOn(ZonedDateTime.now(ZoneId.of(UTC)))
            .build();
    this.clientStatus = ClientStatus.ENABLED;
    validate();
  }

  /**
   * Changes the visibility of the client.
   *
   * @param visibility the new visibility status
   * @param modifiedBy the identifier of the modifier
   */
  public void changeVisibility(ClientVisibility visibility, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    this.clientVisibility = visibility;
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Disables the client.
   *
   * @param modifiedBy the identifier of the modifier
   */
  public void disable(String modifiedBy) {
    changeStatus(ClientStatus.DISABLED, modifiedBy);
  }

  /**
   * Enables the client.
   *
   * @param modifiedBy the identifier of the modifier
   */
  public void enable(String modifiedBy) {
    changeStatus(ClientStatus.ENABLED, modifiedBy);
  }

  /**
   * Invalidates the client, marking it for removal and regenerating the secret.
   *
   * @param modifiedBy the identifier of the modifier
   */
  public void invalidate(String modifiedBy) {
    validateStatus();
    changeStatus(ClientStatus.INVALIDATED, modifiedBy);
    this.secret = new ClientSecret(UUID.randomUUID().toString());
    validate();
  }

  /**
   * Regenerates the client's secret.
   *
   * @param modifiedBy the identifier of the modifier
   */
  public void regenerateSecret(String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    this.secret = new ClientSecret(UUID.randomUUID().toString());
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Encrypts the client's secret using a provided function.
   *
   * @param secretModifier a function that modifies the secret
   */
  public void encryptSecret(Function<String, String> secretModifier) {
    validateStatus();
    validateStatusInvalidated();
    this.secret = new ClientSecret(secretModifier.apply(this.secret.value()));
    validate();
  }

  /**
   * Adds a scope to the client.
   *
   * @param scope the scope to add
   * @param modifiedBy the identifier of the modifier
   */
  public void addScope(String scope, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    validateScope(scope);
    this.scopes.add(scope);
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Removes a scope from the client.
   *
   * @param scope the scope to remove
   * @param modifiedBy the identifier of the modifier
   * @throws ClientDomainException if there is only one scope
   */
  public void removeScope(String scope, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    validateScope(scope);
    if (this.scopes.size() == 1)
      throw new ClientDomainException("Client must have at least one scope");
    this.scopes.remove(scope);
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Updates the client's information.
   *
   * @param newClientInfo the new client information
   * @param modifiedBy the identifier of the modifier
   */
  public void updateClientInfo(ClientInfo newClientInfo, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    if (newClientInfo == null) throw new ClientDomainException("New client info cannot be null");
    this.clientInfo = newClientInfo;
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Updates the client's website information.
   *
   * @param newClientWebsiteInfo the new client website information
   * @param modifiedBy the identifier of the modifier
   */
  public void updateClientWebsiteInfo(ClientWebsiteInfo newClientWebsiteInfo, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    if (newClientWebsiteInfo == null)
      throw new ClientDomainException("New client website info cannot be null");
    this.clientWebsiteInfo = newClientWebsiteInfo;
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Updates the client's redirect information.
   *
   * @param newClientRedirectInfo the new client redirect information
   * @param modifiedBy the identifier of the modifier
   */
  public void updateClientRedirectInfo(
      ClientRedirectInfo newClientRedirectInfo, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    if (newClientRedirectInfo == null)
      throw new ClientDomainException("New client redirect info cannot be null");
    this.clientRedirectInfo = newClientRedirectInfo;
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Adds an authentication method to the client.
   *
   * @param method the authentication method to add
   * @param modifiedBy the identifier of the modifier
   */
  public void addAuthenticationMethod(AuthenticationMethod method, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    if (method == null) throw new ClientDomainException("Authentication method cannot be null");
    this.authenticationMethods.add(method);
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Removes an authentication method from the client.
   *
   * @param method the authentication method to remove
   * @param modifiedBy the identifier of the modifier
   * @throws ClientDomainException if there is only one authentication method
   */
  public void removeAuthenticationMethod(AuthenticationMethod method, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    if (method == null) throw new ClientDomainException("Authentication method cannot be null");
    if (this.authenticationMethods.size() == 1 && this.authenticationMethods.contains(method))
      throw new ClientDomainException(
          "Client must have at least one authentication method. Cannot remove the last one");
    this.authenticationMethods.remove(method);
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Changes the client's status.
   *
   * @param newStatus the new client status
   * @param modifiedBy the identifier of the modifier
   */
  private void changeStatus(ClientStatus newStatus, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    this.clientStatus = newStatus;
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Validates the client state to ensure all required fields are set.
   *
   * @throws ClientDomainException if the client state is invalid
   */
  protected void validate() {
    if (this.id == null || this.id.getValue() == null)
      throw new ClientDomainException("Client is in invalid state due to missing client id");
    if (this.secret == null || this.secret.value() == null)
      throw new ClientDomainException("Client is in invalid state due to missing client secret");
    if (this.authenticationMethods == null || this.authenticationMethods.isEmpty())
      throw new ClientDomainException(
          "Client is in invalid state due to missing authentication methods");
    if (this.scopes == null || this.scopes.isEmpty())
      throw new ClientDomainException("Client is in invalid state due to missing scopes");
    if (this.clientInfo == null)
      throw new ClientDomainException("Client is in invalid state due to missing client info");
    if (this.clientTenantInfo == null)
      throw new ClientDomainException(
          "Client is in invalid state due to missing client tenant info");
    if (this.clientRedirectInfo == null)
      throw new ClientDomainException(
          "Client is in invalid state due to missing client redirect info");
    if (this.clientCreationInfo == null
        || this.clientCreationInfo.getCreatedBy() == null
        || this.clientCreationInfo.getCreatedBy().isBlank())
      throw new ClientDomainException("Client is in invalid state due to invalid creator name");
    if (this.clientCreationInfo.getCreatedOn() == null)
      throw new ClientDomainException("Client is in invalid state due to missing creation date");
    if (this.clientVisibility == null)
      throw new ClientDomainException("Client must have a valid visibility level");
  }

  /**
   * Validates the client's status.
   *
   * @throws ClientDomainException if the client has not been initialized yet
   */
  private void validateStatus() {
    if (this.clientStatus == null)
      throw new ClientDomainException("Client has not been initialized yet");
  }

  /**
   * Validates if the client status is invalidated.
   *
   * @throws ClientDomainException if the client has been marked for removal
   */
  private void validateStatusInvalidated() {
    if (this.clientStatus == ClientStatus.INVALIDATED)
      throw new ClientDomainException("Client has been marked for removal");
  }

  /**
   * Validates a scope.
   *
   * @param scope the scope to validate
   * @throws ClientDomainException if the scope is null or blank
   */
  private void validateScope(String scope) {
    if (scope == null || scope.isBlank())
      throw new ClientDomainException("Scope cannot be null or blank");
  }

  /**
   * Updates the client's modification information.
   *
   * @param modifiedBy the identifier of the modifier
   */
  private void updateModificationInfo(String modifiedBy) {
    this.clientModificationInfo =
        ClientModificationInfo.Builder.builder()
            .modifiedOn(ZonedDateTime.now(ZoneId.of(UTC)))
            .modifiedBy(modifiedBy)
            .build();
  }

  // Getters for various fields

  /**
   * Returns the client ID.
   *
   * @return the client ID
   */
  public ClientId getId() {
    return this.id;
  }

  /**
   * Returns the client secret.
   *
   * @return the client secret
   */
  public ClientSecret getSecret() {
    return this.secret;
  }

  /**
   * Returns the authentication methods.
   *
   * @return the authentication methods
   */
  public Set<AuthenticationMethod> getAuthenticationMethods() {
    return Collections.unmodifiableSet(this.authenticationMethods);
  }

  /**
   * Returns the client scopes.
   *
   * @return the client scopes
   */
  public Set<String> getScopes() {
    return Collections.unmodifiableSet(this.scopes);
  }

  /**
   * Returns the client information.
   *
   * @return the client information
   */
  public ClientInfo getClientInfo() {
    return this.clientInfo;
  }

  /**
   * Returns the client tenant information.
   *
   * @return the client tenant information
   */
  public ClientTenantInfo getClientTenantInfo() {
    return this.clientTenantInfo;
  }

  /**
   * Returns the client website information.
   *
   * @return the client website information
   */
  public ClientWebsiteInfo getClientWebsiteInfo() {
    return this.clientWebsiteInfo;
  }

  /**
   * Returns the client redirect information.
   *
   * @return the client redirect information
   */
  public ClientRedirectInfo getClientRedirectInfo() {
    return this.clientRedirectInfo;
  }

  /**
   * Returns the client creation information.
   *
   * @return the client creation information
   */
  public ClientCreationInfo getClientCreationInfo() {
    return this.clientCreationInfo;
  }

  /**
   * Returns the client modification information.
   *
   * @return the client modification information
   */
  public ClientModificationInfo getClientModificationInfo() {
    return this.clientModificationInfo;
  }

  /**
   * Returns the client status.
   *
   * @return the client status
   */
  public ClientStatus getStatus() {
    return this.clientStatus;
  }

  /**
   * Returns the client visibility.
   *
   * @return the client visibility
   */
  public ClientVisibility getVisibility() {
    return this.clientVisibility;
  }

  /**
   * Returns the client version.
   *
   * @return the client version
   */
  public int getVersion() {
    return this.version;
  }

  /** Builder class for creating instances of {@link Client}. */
  public static final class Builder {
    private ClientId id;
    private ClientSecret secret;
    private Set<AuthenticationMethod> authenticationMethods;
    private Set<String> scopes;
    private ClientInfo clientInfo;
    private ClientTenantInfo clientTenantInfo;
    private ClientWebsiteInfo clientWebsiteInfo;
    private ClientRedirectInfo clientRedirectInfo;
    private ClientCreationInfo clientCreationInfo;
    private ClientModificationInfo clientModificationInfo;
    private ClientStatus clientStatus;
    private ClientVisibility clientVisibility;
    private int version;

    private Builder() {
      this.authenticationMethods = new HashSet<>();
    }

    /**
     * Returns a new Builder instance.
     *
     * @return a new Builder instance
     */
    public static Builder builder() {
      return new Builder();
    }

    public Builder id(ClientId val) {
      this.id = val;
      return this;
    }

    public Builder secret(ClientSecret val) {
      this.secret = val;
      return this;
    }

    public Builder authenticationMethods(Set<AuthenticationMethod> val) {
      this.authenticationMethods = val;
      return this;
    }

    public Builder scopes(Set<String> val) {
      this.scopes = val;
      return this;
    }

    public Builder clientInfo(ClientInfo val) {
      this.clientInfo = val;
      return this;
    }

    public Builder clientTenantInfo(ClientTenantInfo val) {
      this.clientTenantInfo = val;
      return this;
    }

    public Builder clientWebsiteInfo(ClientWebsiteInfo val) {
      this.clientWebsiteInfo = val;
      return this;
    }

    public Builder clientRedirectInfo(ClientRedirectInfo val) {
      this.clientRedirectInfo = val;
      return this;
    }

    public Builder clientCreationInfo(ClientCreationInfo val) {
      this.clientCreationInfo = val;
      return this;
    }

    public Builder clientModificationInfo(ClientModificationInfo val) {
      this.clientModificationInfo = val;
      return this;
    }

    public Builder clientStatus(ClientStatus val) {
      this.clientStatus = val;
      return this;
    }

    public Builder clientVisibility(ClientVisibility val) {
      this.clientVisibility = val;
      return this;
    }

    public Builder clientVersion(int val) {
      this.version = val;
      return this;
    }

    /**
     * Builds and returns a new Client instance.
     *
     * @return a new Client instance
     */
    public Client build() {
      if (this.authenticationMethods.isEmpty())
        this.authenticationMethods.add(AuthenticationMethod.DEFAULT_AUTHENTICATION);
      return new Client(this);
    }
  }
}
