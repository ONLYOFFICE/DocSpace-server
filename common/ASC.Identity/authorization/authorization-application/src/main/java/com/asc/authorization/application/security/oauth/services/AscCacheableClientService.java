package com.asc.authorization.application.security.oauth.services;

import com.asc.authorization.application.mapper.ClientMapper;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;

/** Service class for handling cached operations related to registered clients. */
@Slf4j
@Service
@RequiredArgsConstructor
public class AscCacheableClientService implements CacheableRegisteredClientQueryService {
  private final ClientMapper clientMapper;
  private final JpaClientRepository jpaClientRepository;
  private final EncryptionService encryptionService;

  /**
   * Finds a client by its ID with caching support.
   *
   * @param id the ID of the client.
   * @return the ClientResponse containing the client's details or null.
   */
  @Cacheable(value = "clients", key = "#id", unless = "#result == null")
  public ClientResponse findById(String id) {
    var result = jpaClientRepository.findById(id);
    if (result.isEmpty()) return null;

    var client = clientMapper.toClientResponse(result.get());
    client.setClientSecret(encryptionService.decrypt(client.getClientSecret()));
    return client;
  }

  /**
   * Finds a client by its client ID with caching support.
   *
   * @param clientId the client ID of the client.
   * @return the ClientResponse containing the client's details or null.
   */
  @Cacheable(value = "clients", key = "#clientId", unless = "#result == null")
  public ClientResponse findByClientId(String clientId) {
    var result = jpaClientRepository.findClientByClientId(clientId);
    if (result.isEmpty()) return null;

    var client = clientMapper.toClientResponse(result.get());
    client.setClientSecret(encryptionService.decrypt(client.getClientSecret()));
    return client;
  }
}
