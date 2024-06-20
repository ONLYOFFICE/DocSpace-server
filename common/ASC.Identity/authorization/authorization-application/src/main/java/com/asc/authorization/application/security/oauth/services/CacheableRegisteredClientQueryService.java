package com.asc.authorization.application.security.oauth.services;

import com.asc.common.service.transfer.response.ClientResponse;

/** Interface for a service that provides cached operations for querying registered clients. */
public interface CacheableRegisteredClientQueryService {

  /**
   * Finds a client by its ID with caching support.
   *
   * @param id the ID of the client.
   * @return the ClientResponse containing the client's details.
   */
  ClientResponse findById(String id);

  /**
   * Finds a client by its client ID with caching support.
   *
   * @param clientId the client ID of the client.
   * @return the ClientResponse containing the client's details.
   */
  ClientResponse findByClientId(String clientId);
}
