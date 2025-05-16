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

package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.Role;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.exception.ClientDomainException;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.dao.OptimisticLockingFailureException;
import org.springframework.retry.annotation.Backoff;
import org.springframework.retry.annotation.Recover;
import org.springframework.retry.annotation.Retryable;
import org.springframework.stereotype.Component;

/**
 * ClientUpdateCommandHandler handles the updates of the existing clients. This component
 * coordinates the client update process by interacting with the domain service, repository, and
 * event publisher.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientUpdateCommandHandler {
  private final AuthorizationMessagePublisher<ClientRemovedEvent> clientMessagePublisher;
  private final ClientCommandRepository clientCommandRepository;
  private final ClientDataMapper clientDataMapper;
  private final ClientDomainService clientDomainService;
  private final ClientQueryRepository clientQueryRepository;
  private final EncryptionService encryptionService;

  /**
   * Regenerates and encrypts the client secret.
   *
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing client and tenant information
   * @return the updated client secret response
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      notRecoverable = {ClientNotFoundException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  public ClientSecretResponse regenerateSecret(
      Audit audit, Role role, RegenerateTenantClientSecretCommand command) {
    log.info("Regenerating client secret");

    var client = getClient(audit, role, command.getClientId(), command.getTenantId());
    var event = clientDomainService.regenerateClientSecret(audit, client);
    var clientSecret = client.getSecret().value();
    client.encryptSecret(encryptionService::encrypt);

    MDC.put("client_secret", client.getSecret().value());
    log.debug("Generated a new secret");
    MDC.remove("client_secret");

    var response =
        clientDataMapper.toClientSecret(clientCommandRepository.updateClient(event, client));
    response.setClientSecret(clientSecret);
    return response;
  }

  /**
   * Fallback for secret regeneration on optimistic locking failure.
   *
   * @param e the optimistic locking exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing client and tenant information
   * @return never returns normally
   * @throws ClientDomainException always thrown due to concurrent access issues
   */
  @Recover
  public ClientSecretResponse recoverRegenerateSecret(
      OptimisticLockingFailureException e,
      Audit audit,
      Role role,
      RegenerateTenantClientSecretCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not regenerate secret for client %s due to concurrent access",
            command.getClientId()));
  }

  /**
   * Generic fallback for secret regeneration failures.
   *
   * @param e the triggering exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing client and tenant information
   * @return never returns normally
   * @throws Exception always rethrows the original exception
   */
  @Recover
  public ClientSecretResponse recoverRegenerateSecret(
      Exception e, Audit audit, Role role, RegenerateTenantClientSecretCommand command)
      throws Exception {
    throw e;
  }

  /**
   * Changes the visibility of a client.
   *
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing client, tenant, and desired visibility status
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      notRecoverable = {ClientNotFoundException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  public void changeVisibility(
      Audit audit, Role role, ChangeTenantClientVisibilityCommand command) {
    log.info("Trying to change client visibility");

    var client = getClient(audit, role, command.getClientId(), command.getTenantId());
    if (command.isPublic()) {
      log.info("Changing client visibility to public");
      var event = clientDomainService.makeClientPublic(audit, client);
      clientCommandRepository.changeVisibilityByTenantIdAndClientId(
          event, client.getClientTenantInfo().tenantId(), client.getId(), command.isPublic());
      return;
    }

    log.info("Changing client visibility to private");
    var event = clientDomainService.makeClientPrivate(audit, client);
    clientCommandRepository.changeVisibilityByTenantIdAndClientId(
        event, client.getClientTenantInfo().tenantId(), client.getId(), command.isPublic());
  }

  /**
   * Fallback for visibility change on optimistic locking failure.
   *
   * @param e the optimistic locking exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command with client and tenant details
   * @throws ClientDomainException always thrown due to concurrent access issues
   */
  @Recover
  public void recoverChangeVisibility(
      OptimisticLockingFailureException e,
      Audit audit,
      Role role,
      ChangeTenantClientVisibilityCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not change visibility for client %s due to concurrent access",
            command.getClientId()));
  }

  /**
   * Generic fallback for visibility change failures.
   *
   * @param e the triggering exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command with client and tenant details
   * @throws Exception always rethrows the original exception
   */
  @Recover
  public void recoverChangeVisibility(
      Exception e, Audit audit, Role role, ChangeTenantClientVisibilityCommand command)
      throws Exception {
    throw e;
  }

  /**
   * Changes the activation status of a client.
   *
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing client, tenant, and desired activation status
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      notRecoverable = {ClientNotFoundException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  public void changeActivation(
      Audit audit, Role role, ChangeTenantClientActivationCommand command) {
    log.info("Trying to change client activation");

    var client = getClient(audit, role, command.getClientId(), command.getTenantId());
    if (command.isEnabled()) {
      log.info("Changing client activation to enabled");
      var event = clientDomainService.enableClient(audit, client);
      clientCommandRepository.changeActivationByTenantIdAndClientId(
          event, client.getClientTenantInfo().tenantId(), client.getId(), command.isEnabled());
      return;
    }

    log.info("Changing client activation to disabled");
    var event = clientDomainService.disableClient(audit, client);
    clientCommandRepository.changeActivationByTenantIdAndClientId(
        event, client.getClientTenantInfo().tenantId(), client.getId(), command.isEnabled());
  }

  /**
   * Fallback for activation change on optimistic locking failure.
   *
   * @param e the optimistic locking exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command with client and tenant details
   * @throws ClientDomainException always thrown due to concurrent access issues
   */
  @Recover
  public void recoverChangeActivation(
      OptimisticLockingFailureException e,
      Audit audit,
      Role role,
      ChangeTenantClientActivationCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not change activation for client %s due to concurrent access",
            command.getClientId()));
  }

  /**
   * Generic fallback for activation change failures.
   *
   * @param e the triggering exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command with client and tenant details
   * @throws Exception always rethrows the original exception
   */
  @Recover
  public void recoverChangeActivation(
      Exception e, Audit audit, Role role, ChangeTenantClientActivationCommand command)
      throws Exception {
    throw e;
  }

  /**
   * Updates client information.
   *
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing updated client information
   * @return the updated client response
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      notRecoverable = {ClientNotFoundException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  public ClientResponse updateClient(Audit audit, Role role, UpdateTenantClientCommand command) {
    log.info("Updating client information");

    var client = getClient(audit, role, command.getClientId(), command.getTenantId());
    clientDomainService.updateClientInfo(
        audit,
        client,
        new ClientInfo(command.getName(), command.getDescription(), command.getLogo()));

    var event =
        clientDomainService.updateClientRedirectInfo(
            audit,
            client,
            new ClientRedirectInfo(
                client.getClientRedirectInfo().redirectUris(),
                command.getAllowedOrigins(),
                client.getClientRedirectInfo().logoutRedirectUris()));

    if (command.isAllowPkce()) {
      clientDomainService.addAuthenticationMethod(
          audit, client, AuthenticationMethod.PKCE_AUTHENTICATION);
      clientDomainService.addAuthenticationMethod(
          audit, client, AuthenticationMethod.DEFAULT_AUTHENTICATION);
    } else {
      clientDomainService.addAuthenticationMethod(
          audit, client, AuthenticationMethod.DEFAULT_AUTHENTICATION);
      clientDomainService.removeAuthenticationMethod(
          audit, client, AuthenticationMethod.PKCE_AUTHENTICATION);
    }

    if (command.isPublic()) clientDomainService.makeClientPublic(audit, client);
    else clientDomainService.makeClientPrivate(audit, client);
    return clientDataMapper.toClientResponse(clientCommandRepository.updateClient(event, client));
  }

  /**
   * Fallback for client update on optimistic locking failure.
   *
   * @param e the optimistic locking exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing updated client information
   * @return never returns normally
   * @throws ClientDomainException always thrown due to concurrent access issues
   */
  @Recover
  public ClientResponse recoverUpdateClient(
      OptimisticLockingFailureException e,
      Audit audit,
      Role role,
      UpdateTenantClientCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not update client %s due to concurrent access", command.getClientId()));
  }

  /**
   * Generic fallback for client update failures.
   *
   * @param e the triggering exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing updated client information
   * @return never returns normally
   * @throws Exception always rethrows the original exception
   */
  @Recover
  public ClientResponse recoverUpdateClient(
      Exception e, Audit audit, Role role, UpdateTenantClientCommand command) throws Exception {
    throw e;
  }

  /**
   * Deletes a client.
   *
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing client and tenant information
   * @return the result of the delete operation, the number of rows affected.
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      notRecoverable = {ClientNotFoundException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  public int deleteClient(Audit audit, Role role, DeleteTenantClientCommand command) {
    log.info("Trying to remove client");

    var client = getClient(audit, role, command.getClientId(), command.getTenantId());
    var event = clientDomainService.deleteClient(audit, client);
    // TODO: Move it later to core application service. Now we depend on getClient check
    clientMessagePublisher.publish(
        ClientRemovedEvent.builder().clientId(command.getClientId()).build());
    return clientCommandRepository.deleteByTenantIdAndClientId(
        event, client.getClientTenantInfo().tenantId(), client.getId());
  }

  /**
   * Fallback for client deletion on optimistic locking failure.
   *
   * @param e the optimistic locking exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing client and tenant information
   * @throws ClientDomainException always thrown due to concurrent access issues
   */
  @Recover
  public void recoverDeleteClient(
      OptimisticLockingFailureException e,
      Audit audit,
      Role role,
      DeleteTenantClientCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not delete client %s due to concurrent access", command.getClientId()));
  }

  /**
   * Deletes all clients associated with a specific user and tenant.
   *
   * <p>This method removes all client entities created by the specified user within the given
   * tenant. It employs retry logic with exponential backoff for handling optimistic locking
   * failures.
   *
   * @param command the command containing user and tenant information
   * @return the number of clients deleted
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      notRecoverable = {ClientNotFoundException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  public int deleteUserClients(DeleteUserClientsCommand command) {
    log.info("Trying to remove user clients");
    return clientCommandRepository.deleteAllByTenantIdAndCreatedBy(
        new TenantId(command.getTenantId()), new UserId(command.getUserId()));
  }

  /**
   * Fallback for user clients deletion on optimistic locking failure.
   *
   * @param e the optimistic locking exception
   * @param command the command containing user and tenant information
   * @throws ClientDomainException always thrown due to concurrent access issues
   */
  @Recover
  public void recoverDeleteUserClients(
      OptimisticLockingFailureException e, DeleteUserClientsCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not delete user %s clients due to concurrent access", command.getUserId()));
  }

  /**
   * Deletes all clients associated with a specific tenant.
   *
   * <p>This method removes all client entities belonging to the specified tenant. It employs retry
   * logic with exponential backoff for handling optimistic locking failures.
   *
   * @param tenantId the tenant identifier
   * @return the number of clients deleted
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      notRecoverable = {ClientNotFoundException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  public int deleteTenantClients(long tenantId) {
    log.info("Trying to remove tenant clients");
    return clientCommandRepository.deleteAllByTenantId(new TenantId(tenantId));
  }

  /**
   * Fallback for tenant clients deletion on optimistic locking failure.
   *
   * @param e the optimistic locking exception
   * @param tenantId the tenant identifier
   * @throws ClientDomainException always thrown due to concurrent access issues
   */
  @Recover
  public void recoverDeleteTenantClients(OptimisticLockingFailureException e, long tenantId) {
    throw new ClientDomainException(
        String.format("Could not delete tenant %d clients due to concurrent access", tenantId));
  }

  /**
   * Generic fallback for client deletion failures.
   *
   * @param e the triggering exception
   * @param audit the audit details
   * @param role the role of the requester
   * @param command the command containing client and tenant information
   * @throws Exception always rethrows the original exception
   */
  @Recover
  public void recoverDeleteClient(
      Exception e, Audit audit, Role role, DeleteTenantClientCommand command) throws Exception {
    throw e;
  }

  /**
   * Retrieves a client based on audit, role, client ID, and tenant ID.
   *
   * @param audit the audit details
   * @param role the role of the requester
   * @param clientId the client ID (UUID string)
   * @param tenantId the tenant identifier
   * @return the found client
   * @throws ClientNotFoundException if no client is found
   */
  private Client getClient(Audit audit, Role role, String clientId, long tenantId) {
    try {
      var cid = new ClientId(UUID.fromString(clientId));
      var tid = new TenantId(tenantId);
      if (role.equals(Role.ROLE_ADMIN))
        return clientQueryRepository
            .findByClientIdAndTenantId(cid, tid)
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found", clientId, tenantId)));
      else if (role.equals(Role.ROLE_USER))
        return clientQueryRepository
            .findByClientIdAndTenantIdAndCreatorId(cid, tid, new UserId(audit.getUserId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d and user %s was not found",
                            clientId, tenantId, audit.getUserEmail())));
      throw new ClientNotFoundException(
          String.format(
              "Client with id %s for tenant %d and user %s was not found",
              clientId, tenantId, audit.getUserId()));
    } catch (IllegalArgumentException e) {
      throw new ClientNotFoundException(
          String.format("Client with id %s was not found. Invalid client id format", clientId));
    }
  }
}
