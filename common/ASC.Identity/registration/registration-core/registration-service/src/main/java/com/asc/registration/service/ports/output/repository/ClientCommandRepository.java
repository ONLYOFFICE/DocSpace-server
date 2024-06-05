package com.asc.registration.service.ports.output.repository;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.registration.core.domain.entity.Client;

/**
 * ClientCommandRepository defines the contract for client-related operations that modify the state
 * of clients. This repository handles saving clients, regenerating client secrets, changing
 * activation states, and deleting clients.
 */
public interface ClientCommandRepository {
  /**
   * Saves a client entity to the repository.
   *
   * @param client The client entity to be saved.
   * @return The saved client entity.
   */
  Client saveClient(Client client);

  /**
   * Regenerates the secret key for a specific client identified by tenant ID and client ID.
   *
   * @param tenantId The tenant ID to which the client belongs.
   * @param clientId The client ID for which the secret is to be regenerated.
   * @return The new client secret.
   */
  String regenerateClientSecretByClientId(TenantId tenantId, ClientId clientId);

  /**
   * Changes the activation state of a specific client identified by tenant ID and client ID.
   *
   * @param tenantId The tenant ID to which the client belongs.
   * @param clientId The client ID for which the activation state is to be changed.
   * @param enabled The new activation state (true for enabled, false for disabled).
   */
  void changeActivation(TenantId tenantId, ClientId clientId, boolean enabled);

  /**
   * Deletes a specific client identified by tenant ID and client ID from the repository.
   *
   * @param tenantId The tenant ID to which the client belongs.
   * @param clientId The client ID of the client to be deleted.
   * @return The number of clients deleted (typically 0 or 1).
   */
  int deleteByTenantAndClientId(TenantId tenantId, ClientId clientId);
}
