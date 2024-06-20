package com.asc.registration.data.client.adapter;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Repository;

/**
 * Adapter class for handling client command operations and mapping between domain and data layers.
 * Implements the {@link ClientCommandRepository} interface.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class ClientCommandRepositoryDomainAdapter implements ClientCommandRepository {
  private static final String UTC = "UTC";

  private final JpaClientRepository jpaClientRepository;
  private final ClientDataAccessMapper clientDataAccessMapper;

  /**
   * Saves a client entity to the database.
   *
   * @param client the client entity to save
   * @return the saved client entity
   */
  public Client saveClient(Client client) {
    log.debug("Persisting a new client");

    var entity = clientDataAccessMapper.toEntity(client);
    var result = jpaClientRepository.save(entity);
    return clientDataAccessMapper.toDomain(result);
  }

  /**
   * Regenerates the secret for a client identified by tenant ID and client ID.
   *
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @return the newly generated client secret
   */
  public String regenerateClientSecretByTenantIdAndClientId(TenantId tenantId, ClientId clientId) {
    log.debug("Regenerating and persisting a new secret");

    var secret = UUID.randomUUID().toString();

    log.debug("Newly generated secret: {}", secret);

    jpaClientRepository.regenerateClientSecretByClientId(
        tenantId.getValue(),
        clientId.getValue().toString(),
        secret,
        ZonedDateTime.now(ZoneId.of(UTC)));

    return secret;
  }

  /**
   * Changes the visibility of a client identified by tenant ID and client ID.
   *
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @param visible the new visibility status
   */
  public void changeVisibilityByTenantIdAndClientId(
      TenantId tenantId, ClientId clientId, boolean visible) {
    log.debug("Persisting client visibility changes");

    jpaClientRepository.changeVisibility(
        tenantId.getValue(),
        clientId.getValue().toString(),
        visible,
        ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Changes the activation status of a client identified by tenant ID and client ID.
   *
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @param enabled the new activation status
   */
  public void changeActivationByTenantIdAndClientId(
      TenantId tenantId, ClientId clientId, boolean enabled) {
    log.debug("Persisting activation changes");

    jpaClientRepository.changeActivation(
        tenantId.getValue(),
        clientId.getValue().toString(),
        enabled,
        ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Deletes a client identified by tenant ID and client ID.
   *
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @return the number of clients deleted (typically 0 or 1)
   */
  public int deleteByTenantIdAndClientId(TenantId tenantId, ClientId clientId) {
    log.debug("Persisting invalidated marker");

    return jpaClientRepository.deleteByClientIdAndTenantId(
        clientId.getValue().toString(), tenantId.getValue());
  }
}
