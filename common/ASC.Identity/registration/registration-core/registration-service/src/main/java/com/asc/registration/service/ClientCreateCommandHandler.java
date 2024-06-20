package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * ClientCreateCommandHandler handles the creation of new clients. This component coordinates the
 * client creation process by interacting with the domain service, encryption service, repository,
 * and event publisher.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientCreateCommandHandler {
  private final ClientCommandRepository clientCommandRepository;
  private final ClientDataMapper clientDataMapper;
  private final ClientDomainService clientDomainService;
  private final DomainEventPublisher<ClientEvent> messagePublisher;
  private final EncryptionService encryptionService;

  /**
   * Creates a new client based on the provided command and audit information.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the details for creating a new client.
   * @return The response containing the created client's details.
   */
  @Transactional(
      timeout = 2,
      rollbackFor = {Exception.class})
  public ClientResponse createClient(Audit audit, CreateTenantClientCommand command) {
    log.info("Trying to create a new client");

    var client = clientDataMapper.toDomain(command);

    var event = clientDomainService.createClient(audit, client);
    var clientSecret = client.getSecret().value();
    client.encryptSecret(encryptionService::encrypt);

    clientCommandRepository.saveClient(client);
    messagePublisher.publish(event);

    var response = clientDataMapper.toClientResponse(client);
    response.setClientSecret(clientSecret);

    return response;
  }
}
