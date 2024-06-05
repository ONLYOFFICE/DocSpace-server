package com.asc.registration.core.domain;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientCreatedEvent;
import com.asc.registration.core.domain.event.ClientDeletedEvent;
import com.asc.registration.core.domain.event.ClientUpdatedEvent;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientWebsiteInfo;
import java.time.ZoneId;
import java.time.ZonedDateTime;

/**
 * CoreClientDomainService implements the {@link ClientDomainService} interface, providing the core
 * domain logic for managing clients.
 */
public class CoreClientDomainService implements ClientDomainService {
  private static final String UTC = "UTC";

  /**
   * Creates a new client.
   *
   * @param audit the audit information related to the creation
   * @param client the client to be created
   * @return an event indicating the client was created
   */
  public ClientCreatedEvent createClient(Audit audit, Client client) {
    client.initialize(audit.getUserEmail());
    return new ClientCreatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Enables an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be enabled
   * @return an event indicating the client was enabled
   */
  public ClientUpdatedEvent enableClient(Audit audit, Client client) {
    client.enable(audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Disables an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be disabled
   * @return an event indicating the client was disabled
   */
  public ClientUpdatedEvent disableClient(Audit audit, Client client) {
    client.disable(audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Invalidates an existing client, marking it for removal.
   *
   * @param audit the audit information related to the update
   * @param client the client to be invalidated
   * @return an event indicating the client was invalidated
   */
  public ClientDeletedEvent invalidateClient(Audit audit, Client client) {
    client.invalidate(audit.getUserEmail());
    return new ClientDeletedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Regenerates the secret for an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client whose secret is to be regenerated
   * @return an event indicating the client's secret was regenerated
   */
  public ClientUpdatedEvent regenerateClientSecret(Audit audit, Client client) {
    client.regenerateSecret(audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Updates the information of an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param clientInfo the new client information
   * @return an event indicating the client information was updated
   */
  public ClientUpdatedEvent updateClientInfo(Audit audit, Client client, ClientInfo clientInfo) {
    client.updateClientInfo(clientInfo, audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Updates the website information of an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param clientWebsiteInfo the new client website information
   * @return an event indicating the client website information was updated
   */
  public ClientUpdatedEvent updateClientWebsiteInfo(
      Audit audit, Client client, ClientWebsiteInfo clientWebsiteInfo) {
    client.updateClientWebsiteInfo(clientWebsiteInfo, audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Updates the redirect information of an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param clientRedirectInfo the new client redirect information
   * @return an event indicating the client redirect information was updated
   */
  public ClientUpdatedEvent updateClientRedirectInfo(
      Audit audit, Client client, ClientRedirectInfo clientRedirectInfo) {
    client.updateClientRedirectInfo(clientRedirectInfo, audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Adds an authentication method to an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param authenticationMethod the authentication method to add
   * @return an event indicating the authentication method was added to the client
   */
  public ClientUpdatedEvent addAuthenticationMethod(
      Audit audit, Client client, AuthenticationMethod authenticationMethod) {
    client.addAuthenticationMethod(authenticationMethod, audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Removes an authentication method from an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param authenticationMethod the authentication method to remove
   * @return an event indicating the authentication method was removed from the client
   */
  public ClientUpdatedEvent removeAuthenticationMethod(
      Audit audit, Client client, AuthenticationMethod authenticationMethod) {
    client.removeAuthenticationMethod(authenticationMethod, audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Adds a scope to an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param scope the scope to add
   * @return an event indicating the scope was added to the client
   */
  public ClientUpdatedEvent addScope(Audit audit, Client client, String scope) {
    client.addScope(scope, audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Removes a scope from an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param scope the scope to remove
   * @return an event indicating the scope was removed from the client
   */
  public ClientUpdatedEvent removeScope(Audit audit, Client client, String scope) {
    client.removeScope(scope, audit.getUserEmail());
    return new ClientUpdatedEvent(audit, client, ZonedDateTime.now(ZoneId.of(UTC)));
  }
}
