package com.asc.registration.service.ports.output.repository;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.Optional;

/**
 * ClientQueryRepository defines the contract for client-related query operations. This repository
 * handles retrieving clients based on various query parameters.
 */
public interface ClientQueryRepository {

  /**
   * Finds a client by its unique client ID and visibility status.
   *
   * @param clientId The unique client ID.
   * @param visibility The visibility status of the client.
   * @return An {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findByIdAndVisibility(ClientId clientId, ClientVisibility visibility);

  /**
   * Finds a client by its unique client ID.
   *
   * @param clientId The unique client ID.
   * @return An {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findById(ClientId clientId);

  /**
   * Finds all public and private clients belonging to a specific tenant, with pagination support.
   *
   * @param tenant The tenant ID to which the clients belong.
   * @param page The page number to retrieve.
   * @param limit The number of clients per page.
   * @return A {@link PageableResponse} containing the clients for the specified tenant.
   */
  PageableResponse<Client> findAllPublicAndPrivateByTenantId(TenantId tenant, int page, int limit);

  /**
   * Finds all clients belonging to a specific tenant, with pagination support.
   *
   * @param tenant The tenant ID to which the clients belong.
   * @param page The page number to retrieve.
   * @param limit The number of clients per page.
   * @return A {@link PageableResponse} containing the clients for the specified tenant.
   */
  PageableResponse<Client> findAllByTenantId(TenantId tenant, int page, int limit);

  /**
   * Finds a client by its unique client ID and tenant ID.
   *
   * @param clientId The unique client ID.
   * @param tenant The tenant ID to which the client belongs.
   * @return An {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findByClientIdAndTenantId(ClientId clientId, TenantId tenant);
}
