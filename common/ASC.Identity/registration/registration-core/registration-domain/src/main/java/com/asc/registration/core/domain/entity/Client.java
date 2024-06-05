package com.asc.registration.core.domain.entity;

import com.asc.common.core.domain.entity.AggregateRoot;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
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
 * Client represents the aggregate root in the domain-driven design context. It contains
 * client-specific information and behavior, managing its lifecycle and state.
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
    secret = new ClientSecret(UUID.randomUUID().toString());
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
    secret = new ClientSecret(UUID.randomUUID().toString());
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
    scopes.add(scope);
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
    if (scopes.size() == 1) throw new ClientDomainException("Client must have at least one scope");
    scopes.remove(scope);
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
    clientInfo = newClientInfo;
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
    clientWebsiteInfo = newClientWebsiteInfo;
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
    clientRedirectInfo = newClientRedirectInfo;
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
    authenticationMethods.add(method);
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
    if (authenticationMethods.size() == 1 && authenticationMethods.contains(method))
      throw new ClientDomainException(
          "Client must have at least one authentication method. Cannot remove the last one");
    authenticationMethods.remove(method);
    updateModificationInfo(modifiedBy);
    validate();
  }

  private void changeStatus(ClientStatus newStatus, String modifiedBy) {
    validateStatus();
    validateStatusInvalidated();
    clientStatus = newStatus;
    updateModificationInfo(modifiedBy);
    validate();
  }

  /**
   * Validates the client state to ensure all required fields are set.
   *
   * @throws ClientDomainException if the client state is invalid
   */
  protected void validate() {
    if (id == null || id.getValue() == null)
      throw new ClientDomainException("Client is in invalid state due to missing client id");
    if (secret == null || secret.value() == null)
      throw new ClientDomainException("Client is in invalid state due to missing client secret");
    if (authenticationMethods == null || authenticationMethods.isEmpty())
      throw new ClientDomainException(
          "Client is in invalid state due to missing authentication methods");
    if (scopes == null || scopes.isEmpty())
      throw new ClientDomainException("Client is in invalid state due to missing scopes");
    if (clientInfo == null)
      throw new ClientDomainException("Client is in invalid state due to missing client info");
    if (clientTenantInfo == null)
      throw new ClientDomainException(
          "Client is in invalid state due to missing client tenant info");
    if (clientRedirectInfo == null)
      throw new ClientDomainException(
          "Client is in invalid state due to missing client redirect info");
    if (clientCreationInfo == null
        || clientCreationInfo.getCreatedBy() == null
        || clientCreationInfo.getCreatedBy().isBlank())
      throw new ClientDomainException("Client is in invalid state due to invalid creator name");
    if (clientCreationInfo.getCreatedOn() == null)
      throw new ClientDomainException("Client is in invalid state due to missing creation date");
  }

  private void validateStatus() {
    if (clientStatus == null)
      throw new ClientDomainException("Client has not been initialized yet");
  }

  private void validateStatusInvalidated() {
    if (clientStatus == ClientStatus.INVALIDATED)
      throw new ClientDomainException("Client has been marked for removal");
  }

  private void validateScope(String scope) {
    if (scope == null || scope.isBlank())
      throw new ClientDomainException("Scope cannot be null or blank");
  }

  private void updateModificationInfo(String modifiedBy) {
    this.clientModificationInfo =
        ClientModificationInfo.Builder.builder()
            .modifiedOn(ZonedDateTime.now(ZoneId.of(UTC)))
            .modifiedBy(modifiedBy)
            .build();
  }

  // Getters for various fields

  public ClientId getId() {
    return id;
  }

  public ClientSecret getSecret() {
    return secret;
  }

  public Set<AuthenticationMethod> getAuthenticationMethods() {
    return Collections.unmodifiableSet(authenticationMethods);
  }

  public Set<String> getScopes() {
    return Collections.unmodifiableSet(scopes);
  }

  public ClientInfo getClientInfo() {
    return clientInfo;
  }

  public ClientTenantInfo getClientTenantInfo() {
    return clientTenantInfo;
  }

  public ClientWebsiteInfo getClientWebsiteInfo() {
    return clientWebsiteInfo;
  }

  public ClientRedirectInfo getClientRedirectInfo() {
    return clientRedirectInfo;
  }

  public ClientCreationInfo getClientCreationInfo() {
    return clientCreationInfo;
  }

  public ClientModificationInfo getClientModificationInfo() {
    return clientModificationInfo;
  }

  public ClientStatus getStatus() {
    return clientStatus;
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

    private Builder() {
      this.authenticationMethods = new HashSet<>();
    }

    public static Builder builder() {
      return new Builder();
    }

    public Builder id(ClientId val) {
      id = val;
      return this;
    }

    public Builder secret(ClientSecret val) {
      secret = val;
      return this;
    }

    public Builder authenticationMethods(Set<AuthenticationMethod> val) {
      authenticationMethods = val;
      return this;
    }

    public Builder scopes(Set<String> val) {
      scopes = val;
      return this;
    }

    public Builder clientInfo(ClientInfo val) {
      clientInfo = val;
      return this;
    }

    public Builder clientTenantInfo(ClientTenantInfo val) {
      clientTenantInfo = val;
      return this;
    }

    public Builder clientWebsiteInfo(ClientWebsiteInfo val) {
      clientWebsiteInfo = val;
      return this;
    }

    public Builder clientRedirectInfo(ClientRedirectInfo val) {
      clientRedirectInfo = val;
      return this;
    }

    public Builder clientCreationInfo(ClientCreationInfo val) {
      clientCreationInfo = val;
      return this;
    }

    public Builder clientModificationInfo(ClientModificationInfo val) {
      clientModificationInfo = val;
      return this;
    }

    public Builder clientStatus(ClientStatus val) {
      clientStatus = val;
      return this;
    }

    public Client build() {
      if (this.authenticationMethods.isEmpty())
        this.authenticationMethods.add(AuthenticationMethod.DEFAULT_AUTHENTICATION);
      return new Client(this);
    }
  }
}
