package com.asc.registration.service;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.utilities.cipher.EncryptionService;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.request.fetch.TenantClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.UUID;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

// Bad practice to use transactional for simple selects
// due to round trips.
// But need to timeout the transaction atm.
// TODO: Use some sort of Resilience4j's timeouts
/** Handles client-related queries. */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientQueryHandler {
  private final EncryptionService encryptionService;
  private final ClientQueryRepository clientQueryRepository;
  private final ClientDataMapper clientDataMapper;

  /**
   * Retrieves a client by tenant and client ID.
   *
   * @param query the query containing tenant ID and client ID
   * @return the response containing client details
   */
  @Transactional(timeout = 2)
  public ClientResponse getClient(TenantClientQuery query) {
    log.info("Trying to get an active client by client id");

    var client =
        clientQueryRepository
            .findClientByClientIdAndTenant(
                new ClientId(UUID.fromString(query.getClientId())),
                new TenantId(query.getTenantId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found",
                            query.getClientId(), query.getTenantId())));
    var response = clientDataMapper.toClientResponse(client);
    response.setClientSecret(encryptionService.decrypt(response.getClientSecret()));

    log.info("Decrypting client secret");
    return response;
  }

  /**
   * Retrieves basic information of a client by client ID.
   *
   * @param query the query containing client ID
   * @return the response containing client basic information
   */
  @Transactional(timeout = 2)
  public ClientInfoResponse getClientInfo(TenantClientInfoQuery query) {
    log.info("Trying to get client basic information by client id");

    var client =
        clientQueryRepository
            .findById(new ClientId(UUID.fromString(query.getClientId())))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format("Client with id %s was not found", query.getClientId())));
    return clientDataMapper.toClientInfoResponse(client);
  }

  /**
   * Retrieves all clients for a tenant with pagination.
   *
   * @param query the query containing tenant ID, page, and limit
   * @return the pageable response containing client details
   */
  @Transactional(timeout = 4)
  public PageableResponse<ClientResponse> getClients(TenantClientsPaginationQuery query) {
    log.info("Trying to get all clients by tenant id");

    var result =
        clientQueryRepository.findAllByTenant(
            new TenantId(query.getTenantId()), query.getPage(), query.getLimit());
    return PageableResponse.<ClientResponse>builder()
        .page(result.getPage())
        .limit(result.getLimit())
        .data(
            StreamSupport.stream(result.getData().spliterator(), false)
                .map(clientDataMapper::toClientResponse)
                .peek(c -> c.setClientSecret(encryptionService.decrypt(c.getClientSecret())))
                .collect(Collectors.toSet()))
        .next(result.getNext())
        .previous(result.getPrevious())
        .build();
  }
}
