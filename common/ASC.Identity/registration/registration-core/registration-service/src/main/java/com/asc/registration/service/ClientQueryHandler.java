package com.asc.registration.service;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.request.fetch.ClientInfoPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.ClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
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
// But need to timeout the transaction atm. TODO: Another mechanism to timeout
/** Handles client-related queries. */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientQueryHandler {
  private final ClientDataMapper clientDataMapper;
  private final ClientQueryRepository clientQueryRepository;
  private final EncryptionService encryptionService;

  /**
   * Retrieves a client by tenant and client ID. Should be only accessed by tenant admins.
   *
   * @param query the query containing tenant ID and client ID
   * @return the response containing client details
   */
  @Transactional(timeout = 2)
  public ClientResponse getClient(TenantClientQuery query) {
    log.info("Trying to get an active client by client id");

    var client =
        clientQueryRepository
            .findByClientIdAndTenantId(
                new ClientId(UUID.fromString(query.getClientId())),
                new TenantId(query.getTenantId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found",
                            query.getClientId(), query.getTenantId())));

    var response = clientDataMapper.toClientResponse(client);

    log.info("Decrypting client secret");

    response.setClientSecret(encryptionService.decrypt(response.getClientSecret()));

    return response;
  }

  /**
   * Retrieves basic information of a client by client ID and tenant ID.
   *
   * @param query the query containing client ID
   * @return the response containing client basic information
   */
  @Transactional(timeout = 2)
  public ClientInfoResponse getClientInfo(ClientInfoQuery query) {
    log.info("Trying to get client basic information by client id");

    var client =
        clientQueryRepository
            .findById(new ClientId(UUID.fromString(query.getClientId())))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format("Client with id %s was not found", query.getClientId())));

    var clientTenant = client.getClientTenantInfo().tenantId().getValue();
    var clientVisibility = client.getVisibility();
    if (!clientTenant.equals(query.getTenantId())
        && !clientVisibility.equals(ClientVisibility.PUBLIC))
      throw new ClientNotFoundException(
          String.format(
              "Client with id %s can't be accessed by current user", query.getClientId()));

    return clientDataMapper.toClientInfoResponse(client);
  }

  @Transactional(timeout = 3)
  public PageableResponse<ClientInfoResponse> getClientsInfo(ClientInfoPaginationQuery query) {
    log.info("Trying to get clients information by client id");

    var result =
        clientQueryRepository.findAllPublicAndPrivateByTenantId(
            new TenantId(query.getTenantId()), query.getPage(), query.getLimit());

    return PageableResponse.<ClientInfoResponse>builder()
        .page(result.getPage())
        .limit(result.getLimit())
        .data(
            StreamSupport.stream(result.getData().spliterator(), false)
                .map(clientDataMapper::toClientInfoResponse)
                .collect(Collectors.toSet()))
        .next(result.getNext())
        .previous(result.getPrevious())
        .build();
  }

  /**
   * Retrieves basic information of a client by client ID.
   *
   * @param clientId the query containing client ID
   * @return the response containing client basic information
   */
  @Transactional(timeout = 2)
  public ClientInfoResponse getClientInfo(String clientId) {
    log.info("Trying to get client basic information by client id");

    var client =
        clientQueryRepository
            .findById(new ClientId(UUID.fromString(clientId)))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format("Client with id %s was not found", clientId)));

    return clientDataMapper.toClientInfoResponse(client);
  }

  /**
   * Retrieves all clients for a tenant with pagination. Should be only accessed by tenant admins.
   *
   * @param query the query containing tenant ID, page, and limit
   * @return the pageable response containing client details
   */
  @Transactional(timeout = 3)
  public PageableResponse<ClientResponse> getClients(TenantClientsPaginationQuery query) {
    log.info("Trying to get all clients by tenant id");

    var result =
        clientQueryRepository.findAllByTenantId(
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
