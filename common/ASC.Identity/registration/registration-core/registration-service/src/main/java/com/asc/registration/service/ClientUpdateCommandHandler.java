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

package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.event.ClientEvent;
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
import org.springframework.transaction.annotation.Transactional;

/**
 * ClientUpdateCommandHandler handles the updates of the existing clients. This component
 * coordinates the client update process by interacting with the domain service, repository, and
 * event publisher.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientUpdateCommandHandler {
  private final ClientCommandRepository clientCommandRepository;
  private final ClientDataMapper clientDataMapper;
  private final ClientDomainService clientDomainService;
  private final ClientQueryRepository clientQueryRepository;
  private final DomainEventPublisher<ClientEvent> messagePublisher;
  private final EncryptionService encryptionService;

  /**
   * Regenerates the client secret.
   *
   * @param audit the audit information
   * @param command the command containing client and tenant information
   * @return the client secret response
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  @Transactional(
      timeout = 3,
      rollbackFor = {Exception.class})
  public ClientSecretResponse regenerateSecret(
      Audit audit, RegenerateTenantClientSecretCommand command) {
    log.info("Trying to regenerate client secret");

    var client =
        clientQueryRepository
            .findByClientIdAndTenantId(
                new ClientId(UUID.fromString(command.getClientId())),
                new TenantId(command.getTenantId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found",
                            command.getClientId(), command.getTenantId())));

    var event = clientDomainService.regenerateClientSecret(audit, client);
    var clientSecret = client.getSecret().value();
    client.encryptSecret(encryptionService::encrypt);

    MDC.put("client_secret", client.getSecret().value());
    log.debug("Generated a new secret");
    MDC.remove("client_secret");

    messagePublisher.publish(event);

    var response = clientDataMapper.toClientSecret(clientCommandRepository.updateClient(client));
    response.setClientSecret(clientSecret);

    return response;
  }

  /**
   * Fallback method to handle recovery when the client secret regeneration fails due to an
   * optimistic locking exception.
   *
   * @param e the exception that triggered the recovery
   * @param audit the audit information for tracking the operation
   * @param command the command with client and tenant information for secret regeneration
   * @throws ClientDomainException thrown if the operation cannot recover
   */
  @Recover
  public ClientSecretResponse recoverRegenerateSecret(
      OptimisticLockingFailureException e,
      Audit audit,
      RegenerateTenantClientSecretCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not regenerate client %s secret due to concurrent access",
            command.getClientId()));
  }

  /**
   * Changes the visibility of a client to either public or private based on the command. If the
   * operation encounters an optimistic locking exception, it retries based on the specified retry
   * policy.
   *
   * @param audit the audit information for tracking the operation
   * @param command the command containing client and tenant information along with the desired
   *     visibility status
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  @Transactional(
      timeout = 3,
      rollbackFor = {Exception.class})
  public void changeVisibility(Audit audit, ChangeTenantClientVisibilityCommand command) {
    log.info("Trying to change client visibility");

    var client =
        clientQueryRepository
            .findByClientIdAndTenantId(
                new ClientId(UUID.fromString(command.getClientId())),
                new TenantId(command.getTenantId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found",
                            command.getClientId(), command.getTenantId())));

    if (command.isPublic()) {
      log.info("Changing client visibility to public");
      var event = clientDomainService.makeClientPublic(audit, client);
      clientCommandRepository.updateClient(client);
      messagePublisher.publish(event);
      return;
    }

    log.info("Changing client visibility to private");
    var event = clientDomainService.makeClientPrivate(audit, client);
    clientCommandRepository.updateClient(client);
    messagePublisher.publish(event);
  }

  /**
   * Fallback method to handle recovery when the visibility change operation fails due to an
   * optimistic locking exception.
   *
   * @param e the exception that triggered the recovery
   * @param audit the audit information for tracking the operation
   * @param command the command with client and tenant information for visibility change
   * @throws ClientDomainException thrown if the operation cannot recover
   */
  @Recover
  public void recoverChangeVisibility(
      OptimisticLockingFailureException e,
      Audit audit,
      ChangeTenantClientVisibilityCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not change client %s visibility due to concurrent access",
            command.getClientId()));
  }

  /**
   * Changes the activation status of a client.
   *
   * @param audit the audit information
   * @param command the command containing client, tenant information, and the desired activation
   *     status
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  @Transactional(
      timeout = 3,
      rollbackFor = {Exception.class})
  public void changeActivation(Audit audit, ChangeTenantClientActivationCommand command) {
    log.info("Trying to change client activation");

    var client =
        clientQueryRepository
            .findByClientIdAndTenantId(
                new ClientId(UUID.fromString(command.getClientId())),
                new TenantId(command.getTenantId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found",
                            command.getClientId(), command.getTenantId())));

    if (command.isEnabled()) {
      log.info("Changing client activation to enabled");
      var event = clientDomainService.enableClient(audit, client);
      clientCommandRepository.updateClient(client);
      messagePublisher.publish(event);
      return;
    }

    log.info("Changing client activation to disabled");
    var event = clientDomainService.disableClient(audit, client);
    clientCommandRepository.updateClient(client);
    messagePublisher.publish(event);
  }

  /**
   * Fallback method to handle recovery when the activation change fails due to an optimistic
   * locking exception.
   *
   * @param e the exception that triggered the recovery
   * @param audit the audit information for tracking the operation
   * @param command the command containing tenant and client information for activation change
   * @throws ClientDomainException thrown if the operation cannot recover
   */
  @Recover
  public void recoverChangeActivation(
      OptimisticLockingFailureException e,
      Audit audit,
      ChangeTenantClientActivationCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not change client %s activation due to concurrent access",
            command.getClientId()));
  }

  /**
   * Updates the client information.
   *
   * @param audit the audit information
   * @param command the command containing client and tenant information along with updated client
   *     details
   * @return the updated client response
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  @Transactional(
      timeout = 3,
      rollbackFor = {Exception.class})
  public ClientResponse updateClient(Audit audit, UpdateTenantClientCommand command) {
    log.info("Trying to update client info");

    var client =
        clientQueryRepository
            .findByClientIdAndTenantId(
                new ClientId(UUID.fromString(command.getClientId())),
                new TenantId(command.getTenantId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found",
                            command.getClientId(), command.getTenantId())));

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
    messagePublisher.publish(event);
    return clientDataMapper.toClientResponse(clientCommandRepository.updateClient(client));
  }

  /**
   * Fallback method to handle recovery when the client update operation fails due to an optimistic
   * locking exception.
   *
   * @param e the exception that triggered the recovery
   * @param audit the audit information for tracking the operation
   * @param command the command containing tenant and client information along with updated client
   *     details
   * @throws ClientDomainException thrown if the operation cannot recover
   */
  @Recover
  public ClientResponse recoverUpdateClient(
      OptimisticLockingFailureException e, Audit audit, UpdateTenantClientCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not update client with id %s due to concurrent access", command.getClientId()));
  }

  /**
   * Deletes a client.
   *
   * @param audit the audit information
   * @param command the command containing client and tenant information
   */
  @Retryable(
      retryFor = {OptimisticLockingFailureException.class},
      backoff = @Backoff(value = 500, multiplier = 1.65))
  @Transactional(
      timeout = 3,
      rollbackFor = {Exception.class})
  public void deleteClient(Audit audit, DeleteTenantClientCommand command) {
    log.info("Trying to remove client");

    var client =
        clientQueryRepository
            .findByClientIdAndTenantId(
                new ClientId(UUID.fromString(command.getClientId())),
                new TenantId(command.getTenantId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found",
                            command.getClientId(), command.getTenantId())));

    var event = clientDomainService.invalidateClient(audit, client);
    messagePublisher.publish(event);
    clientCommandRepository.updateClient(client);
  }

  /**
   * Fallback method to handle recovery when the client deletion fails due to an optimistic locking
   * exception.
   *
   * @param e the exception that triggered the recovery
   * @param audit the audit information for tracking the operation
   * @param command the command containing tenant and client information for deletion
   * @throws ClientDomainException thrown if the operation cannot recover
   */
  @Recover
  public void recoverDeleteClient(
      OptimisticLockingFailureException e, Audit audit, DeleteTenantClientCommand command) {
    throw new ClientDomainException(
        String.format(
            "Could not delete client with id %s due to concurrent access", command.getClientId()));
  }
}
