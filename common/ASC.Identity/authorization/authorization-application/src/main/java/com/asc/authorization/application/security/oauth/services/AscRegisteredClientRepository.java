package com.asc.authorization.application.security.oauth.services;

import com.asc.authorization.application.exception.RegisteredClientPermissionException;
import com.asc.authorization.application.mapper.ClientMapper;
import com.asc.common.core.domain.exception.DomainNotFoundException;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.common.utilities.cipher.EncryptionService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository for managing registered OAuth2 clients, providing methods to find clients by ID or
 * client ID.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class AscRegisteredClientRepository implements RegisteredClientRepository {
  private final JpaClientRepository jpaClientRepository;
  private final EncryptionService encryptionService;
  private final ClientMapper clientMapper;

  /**
   * This method is not supported as the repository only supports read operations.
   *
   * @param registeredClient the registered client to save.
   */
  public void save(RegisteredClient registeredClient) {
    MDC.put("client_id", registeredClient.getClientId());
    MDC.put("client_name", registeredClient.getClientName());
    log.error("ASC registered client repository supports only read operations");
    MDC.clear();
  }

  /**
   * Finds a registered client by its ID.
   *
   * @param id the ID of the registered client.
   * @return the RegisteredClient object if found, or null if not found.
   */
  @Cacheable(
      cacheNames = {"identityClients"},
      cacheManager = "clientCacheManager")
  public RegisteredClient findById(String id) {
    try {
      MDC.put("client_id", id);
      log.info("Trying to find registered client by id");

      var result = jpaClientRepository.findById(id);
      if (result.isEmpty())
        throw new DomainNotFoundException("Could not find client with id " + id);
      var client = result.get();
      client.setClientSecret(encryptionService.decrypt(client.getClientSecret()));
      return clientMapper.toRegisteredClient(client);
    } catch (Exception e) {
      log.warn("Could not find registered client", e);
      return null;
    } finally {
      MDC.clear();
    }
  }

  /**
   * Finds a registered client by its client ID.
   *
   * @param clientId the client ID of the registered client.
   * @return the RegisteredClient object if found, or null if not found.
   */
  @Cacheable(
      cacheNames = {"identityClients"},
      cacheManager = "clientCacheManager")
  public RegisteredClient findByClientId(String clientId) {
    try {
      MDC.put("client_id", clientId);
      log.info("Trying to get client by client id");

      var result = jpaClientRepository.findClientByClientId(clientId);
      if (result.isEmpty())
        throw new DomainNotFoundException("Could not find client with client_id " + clientId);

      var client = result.get();
      if (!client.isEnabled())
        throw new RegisteredClientPermissionException(
            String.format("client with client_id %s is disabled", clientId));

      client.setClientSecret(encryptionService.decrypt(client.getClientSecret()));
      return clientMapper.toRegisteredClient(client);
    } catch (Exception e) {
      log.warn("Could not get client by client_id", e);
      return null;
    } finally {
      MDC.clear();
    }
  }
}
