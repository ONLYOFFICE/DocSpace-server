package com.asc.registration.data.client.adapter;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.Optional;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Repository;

/**
 * Adapter class for handling client query operations and mapping between domain and data layers.
 * Implements the {@link ClientQueryRepository} interface.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class ClientQueryRepositoryDomainAdapter implements ClientQueryRepository {
  private final JpaClientRepository jpaClientRepository;
  private final ClientDataAccessMapper clientDataAccessMapper;

  /**
   * Finds a client by its ID and visibility.
   *
   * @param clientId the client ID
   * @param visibility the visibility status of the client
   * @return an optional containing the found client, or empty if not found
   */
  public Optional<Client> findByIdAndVisibility(ClientId clientId, ClientVisibility visibility) {
    log.debug("Querying client by client id and visibility");
    return jpaClientRepository
        .findByIdAndVisibility(
            clientId.getValue().toString(), visibility.equals(ClientVisibility.PUBLIC))
        .map(clientDataAccessMapper::toDomain);
  }

  /**
   * Finds a client by its ID.
   *
   * @param clientId the client ID
   * @return an optional containing the found client, or empty if not found
   */
  public Optional<Client> findById(ClientId clientId) {
    log.debug("Querying client by client id");
    return jpaClientRepository
        .findById(clientId.getValue().toString())
        .map(clientDataAccessMapper::toDomain);
  }

  /**
   * Finds all public and private clients by tenant ID with pagination.
   *
   * @param tenant the tenant ID
   * @param page the page number
   * @param limit the number of clients per page
   * @return a pageable response containing the clients
   */
  public PageableResponse<Client> findAllPublicAndPrivateByTenantId(
      TenantId tenant, int page, int limit) {
    log.debug("Querying all public and private clients by tenant id with pagination");
    var clients =
        jpaClientRepository.findAllPublicAndPrivateByTenant(
            tenant.getValue(), Pageable.ofSize(limit).withPage(page));

    var builder =
        PageableResponse.<Client>builder()
            .page(page)
            .limit(limit)
            .data(
                clients.stream()
                    .filter(c -> !c.isInvalidated())
                    .map(clientDataAccessMapper::toDomain)
                    .collect(Collectors.toSet()));

    if (clients.hasPrevious()) builder.previous(page - 1);
    if (clients.hasNext()) builder.next(page + 1);

    return builder.build();
  }

  /**
   * Finds all clients by tenant ID with pagination.
   *
   * @param tenant the tenant ID
   * @param page the page number
   * @param limit the number of clients per page
   * @return a pageable response containing the clients
   */
  public PageableResponse<Client> findAllByTenantId(TenantId tenant, int page, int limit) {
    log.debug("Querying clients by tenant id with pagination");
    var clients =
        jpaClientRepository.findAllByTenantId(
            tenant.getValue(), Pageable.ofSize(limit).withPage(page));

    var builder =
        PageableResponse.<Client>builder()
            .page(page)
            .limit(limit)
            .data(
                clients.stream()
                    .filter(c -> !c.isInvalidated())
                    .map(clientDataAccessMapper::toDomain)
                    .collect(Collectors.toSet()));

    if (clients.hasPrevious()) builder.previous(page - 1);
    if (clients.hasNext()) builder.next(page + 1);

    return builder.build();
  }

  /**
   * Finds a client by its client ID and tenant ID.
   *
   * @param clientId the client ID
   * @param tenant the tenant ID
   * @return an optional containing the found client, or empty if not found
   */
  public Optional<Client> findByClientIdAndTenantId(ClientId clientId, TenantId tenant) {
    log.debug("Querying client by client id and tenant id");
    return jpaClientRepository
        .findClientByClientIdAndTenantId(clientId.getValue().toString(), tenant.getValue())
        .map(clientDataAccessMapper::toDomain);
  }
}