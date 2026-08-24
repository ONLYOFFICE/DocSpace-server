// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.authorization.application.security.oauth.service;

import com.asc.authorization.application.exception.client.RegisteredClientPermissionException;
import com.asc.authorization.application.mapper.ClientMapper;
import com.asc.authorization.data.client.cache.CachedRegisteredClient;
import com.asc.authorization.data.client.cache.RegisteredClientCacheService;
import com.asc.common.utilities.concurrent.SingleFlight;
import java.time.Duration;
import java.util.Optional;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Repository;

/**
 * Service for managing registered OAuth2 clients.
 *
 * <p>This service acts as a repository for read-only operations on registered clients. It interacts
 * with a gRPC service to fetch client information and provides methods for retrieving clients by ID
 * or client ID. It also validates client accessibility based on specific conditions.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class RegisteredClientService
    implements RegisteredClientRepository,
        RegisteredClientAccessibilityService,
        RegisteredClientOwnerService {
  private static final Duration RESOLUTION_WAIT = Duration.ofMillis(1500);
  private final SingleFlight<String, CachedRegisteredClient> calls =
      new SingleFlight<>(RESOLUTION_WAIT);

  private final RegisteredClientCacheService registeredClientCacheService;
  private final GrpcRegisteredClientService grpcRegisteredClientService;
  private final ClientMapper clientMapper;

  /**
   * Fetches a client from the gRPC service and caches it for subsequent lookups.
   *
   * @param clientId the client ID to fetch.
   * @return the fetched snapshot of the client.
   */
  private CachedRegisteredClient fetchAndCache(String clientId) {
    var client = grpcRegisteredClientService.getClient(clientId);
    var cachedClient = clientMapper.toCachedRegisteredClient(client);
    registeredClientCacheService.put(cachedClient);

    return cachedClient;
  }

  /**
   * Resolves a client from the cache, falling back to the gRPC service and populating the cache on
   * a miss.
   *
   * @param clientId the client ID to resolve.
   * @return the cached snapshot of the client.
   */
  private CachedRegisteredClient resolveCachedClient(String clientId) {
    var cachedClient = registeredClientCacheService.get(clientId).orElse(null);
    if (cachedClient != null) return cachedClient;

    return calls.execute(clientId, () -> fetchAndCache(clientId));
  }

  /**
   * Saves a registered client.
   *
   * <p>This operation is not supported because the service is read-only.
   *
   * @param registeredClient the {@link RegisteredClient} to save.
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
   * <p>The cache is consulted first; on a miss, the client is retrieved from the gRPC service and
   * the cache is populated for subsequent lookups. If the client is disabled, a {@link
   * RegisteredClientPermissionException} is thrown. If the client is not found, null is returned.
   *
   * @param id the ID of the registered client.
   * @return the {@link RegisteredClient}, or {@code null} if not found.
   */
  public RegisteredClient findById(String id) {
    try {
      MDC.put("client_id", id);
      log.info("Trying to find registered client by id");

      var cachedClient = resolveCachedClient(id);
      if (!cachedClient.isEnabled())
        throw new RegisteredClientPermissionException(
            String.format("Client with id %s is disabled", id));

      return clientMapper.toRegisteredClient(cachedClient);
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
   * <p>This method delegates to {@link #findById(String)} to retrieve the client. If the client is
   * not found, null is returned.
   *
   * @param clientId the client ID of the registered client.
   * @return the {@link RegisteredClient}, or {@code null} if not found.
   */
  public RegisteredClient findByClientId(String clientId) {
    try {
      MDC.put("client_id", clientId);
      log.info("Trying to get client by client id");

      return findById(clientId);
    } catch (Exception e) {
      log.warn("Could not get client by client_id", e);
      return null;
    } finally {
      MDC.clear();
    }
  }

  /**
   * Loads a registered client, preferring the cache, when it is public and enabled.
   *
   * @param clientId the client ID of the registered client
   * @return the accessible client, or empty if missing, private, or disabled
   */
  public Optional<RegisteredClient> findAccessibleClient(String clientId) {
    try {
      var cachedClient = resolveCachedClient(clientId);
      if (!cachedClient.isPublicClient() || !cachedClient.isEnabled()) return Optional.empty();

      return Optional.of(clientMapper.toRegisteredClient(cachedClient));
    } catch (Exception e) {
      log.warn("Registered client not found for client ID: {}", clientId);
      return Optional.empty();
    }
  }

  /**
   * Resolves the owner of a client from the cache, falling back to the gRPC service on a miss.
   *
   * @param clientId the client ID whose owner to resolve.
   * @return the owner, or empty if the client could not be resolved.
   */
  public Optional<Owner> findClientOwner(String clientId) {
    try {
      var cachedClient = resolveCachedClient(clientId);
      return Optional.of(new Owner(cachedClient.getTenantId(), cachedClient.getCreatedBy()));
    } catch (Exception e) {
      log.warn("Could not resolve the owner of client ID: {}", clientId);
      return Optional.empty();
    }
  }
}
