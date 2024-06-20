package com.asc.authorization.application.security.oauth.services;

import com.asc.authorization.application.exception.client.RegisteredClientPermissionException;
import com.asc.authorization.application.mapper.ClientMapper;
import com.asc.common.core.domain.exception.DomainNotFoundException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
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
public class AscRegisteredClientService
    implements RegisteredClientRepository, RegisteredClientAccessibilityService {
  private final AscCacheableClientService cacheableClientService;
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
   * @throws DomainNotFoundException if the client is not found
   */
  public RegisteredClient findById(String id) {
    try {
      MDC.put("client_id", id);
      log.info("Trying to find registered client by id");

      var client = cacheableClientService.findById(id);
      if (client == null)
        throw new DomainNotFoundException(String.format("Client with id %s was no found", id));

      if (!client.isEnabled())
        throw new RegisteredClientPermissionException(
            String.format("Client with id %s is disabled", id));

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
   * @throws DomainNotFoundException if the client is not found
   * @throws RegisteredClientPermissionException if the client is disabled
   */
  public RegisteredClient findByClientId(String clientId) {
    try {
      MDC.put("client_id", clientId);
      log.info("Trying to get client by client id");

      var client = cacheableClientService.findByClientId(clientId);
      if (client == null)
        throw new DomainNotFoundException(
            String.format("Client with client_id %s was no found", clientId));

      if (!client.isEnabled())
        throw new RegisteredClientPermissionException(
            String.format("Client with client_id %s is disabled", clientId));

      return clientMapper.toRegisteredClient(client);
    } catch (Exception e) {
      log.warn("Could not get client by client_id", e);
      return null;
    } finally {
      MDC.clear();
    }
  }

  /**
   * Validates the accessibility of the client associated with the given tenant.
   *
   * @param clientId the id of the registered client
   * @param tenantId the tenant of the current caller to validate accessibility against
   * @return true if the client is accessible, false otherwise
   */
  public boolean validateClientAccessibility(String clientId, int tenantId) {
    var client = cacheableClientService.findByClientId(clientId);

    if (client == null) {
      log.warn("Registered client not found for client ID: {}", clientId);
      return false;
    }

    if (tenantId != client.getTenant() && !client.isPublic()) {
      log.warn(
          "Client {} is not accessible and does not belong to this tenant", client.getClientId());
      return false;
    }

    if (!client.isEnabled()) {
      log.warn("Client {} is disabled", client.getClientId());
      return false;
    }

    return true;
  }
}
