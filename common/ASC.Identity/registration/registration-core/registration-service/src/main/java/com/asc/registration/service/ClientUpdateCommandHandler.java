package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.utilities.cipher.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.request.update.ChangeTenantClientActivationCommand;
import com.asc.registration.service.transfer.request.update.DeleteTenantClientCommand;
import com.asc.registration.service.transfer.request.update.RegenerateTenantClientSecretCommand;
import com.asc.registration.service.transfer.request.update.UpdateTenantClientCommand;
import com.asc.registration.service.transfer.response.ClientResponse;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Isolation;
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
  private final ClientDomainService clientDomainService;
  private final ClientQueryRepository clientQueryRepository;
  private final ClientCommandRepository clientCommandRepository;
  private final EncryptionService encryptionService;
  private final DomainEventPublisher<ClientEvent> messagePublisher;
  private final ClientDataMapper clientDataMapper;

  /**
   * Regenerates the client secret.
   *
   * @param audit the audit information
   * @param command the command containing client and tenant information
   * @return the client secret response
   */
  @Transactional(timeout = 2, isolation = Isolation.REPEATABLE_READ)
  public ClientSecretResponse regenerateSecret(
      Audit audit, RegenerateTenantClientSecretCommand command) {
    log.info("Trying to regenerate client secret");

    var client =
        clientQueryRepository
            .findClientByClientIdAndTenant(
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
    var response = clientDataMapper.toClientSecret(clientCommandRepository.saveClient(client));
    response.setClientSecret(clientSecret);
    return response;
  }

  /**
   * Changes the activation status of a client.
   *
   * @param audit the audit information
   * @param command the command containing client, tenant information, and the desired activation
   *     status
   */
  @Transactional(timeout = 2, isolation = Isolation.REPEATABLE_READ)
  public void changeActivation(Audit audit, ChangeTenantClientActivationCommand command) {
    log.info("Trying to change client activation");

    var client =
        clientQueryRepository
            .findClientByClientIdAndTenant(
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
      clientCommandRepository.saveClient(client);
      messagePublisher.publish(event);
      return;
    }

    log.info("Changing client activation to disabled");
    var event = clientDomainService.disableClient(audit, client);
    clientCommandRepository.saveClient(client);
    messagePublisher.publish(event);
  }

  /**
   * Updates the client information.
   *
   * @param audit the audit information
   * @param command the command containing client and tenant information along with updated client
   *     details
   * @return the updated client response
   */
  @Transactional(timeout = 2, isolation = Isolation.REPEATABLE_READ)
  public ClientResponse updateClient(Audit audit, UpdateTenantClientCommand command) {
    log.info("Trying to update client info");

    var client =
        clientQueryRepository
            .findClientByClientIdAndTenant(
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

    messagePublisher.publish(event);
    return clientDataMapper.toClientResponse(clientCommandRepository.saveClient(client));
  }

  /**
   * Deletes a client.
   *
   * @param audit the audit information
   * @param command the command containing client and tenant information
   */
  @Transactional(timeout = 2, isolation = Isolation.READ_COMMITTED)
  public void deleteClient(Audit audit, DeleteTenantClientCommand command) {
    log.info("Trying to remove client");

    var client =
        clientQueryRepository
            .findClientByClientIdAndTenant(
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
    clientCommandRepository.saveClient(client);
  }
}
