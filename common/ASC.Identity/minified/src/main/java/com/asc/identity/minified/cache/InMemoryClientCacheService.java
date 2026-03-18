// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.identity.minified.cache;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import com.github.benmanes.caffeine.cache.Cache;
import com.github.benmanes.caffeine.cache.Caffeine;
import java.time.Duration;
import java.util.Optional;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Primary;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;

/**
 * In-memory implementation of {@link ClientCacheService} using Caffeine cache.
 *
 * <p>This implementation is used in the minified deployment where Redis is not available. It
 * provides a local in-memory cache with automatic eviction based on size and time.
 */
@Slf4j
@Service
@Primary
@Profile("minified")
public class InMemoryClientCacheService implements ClientCacheService {
  private static final String CACHE_KEY_SEPARATOR = "_";
  private final Cache<String, Client> cache;

  public InMemoryClientCacheService() {
    this.cache =
        Caffeine.newBuilder()
            .maximumSize(10000)
            .expireAfterWrite(Duration.ofMinutes(5))
            .recordStats()
            .build();
    log.info("Initialized in-memory client cache with Caffeine");
  }

  /**
   * Builds a cache key for a given tenant and client.
   *
   * @param tenantId tenant identifier
   * @param clientId client identifier
   * @return concatenated key in {@code <tenantId>_<clientId>} format
   */
  private String buildCacheKey(TenantId tenantId, ClientId clientId) {
    return tenantId.getValue() + CACHE_KEY_SEPARATOR + clientId.getValue().toString();
  }

  /**
   * Puts the given client into the cache if it contains both ID and tenant information.
   *
   * @param client client aggregate to cache
   */
  @Override
  public void put(Client client) {
    if (client == null || client.getId() == null) {
      log.warn("Attempted to cache null client or client with null ID");
      return;
    }

    if (client.getClientTenantInfo() == null || client.getClientTenantInfo().tenantId() == null) {
      log.warn(
          "Attempted to cache client without tenant information: {}", client.getId().getValue());
      return;
    }

    var key = buildCacheKey(client.getClientTenantInfo().tenantId(), client.getId());
    cache.put(key, client);
    log.debug("Cached client with ID: {}, key: {}", client.getId().getValue(), key);
  }

  /**
   * Retrieves a cached client by its ID and tenant.
   *
   * @param clientId client identifier
   * @param tenantId tenant identifier
   * @return {@link Optional} containing the client if present; otherwise empty
   */
  @Override
  public Optional<Client> get(ClientId clientId, TenantId tenantId) {
    if (clientId == null || tenantId == null) return Optional.empty();

    var key = buildCacheKey(tenantId, clientId);
    var cached = cache.getIfPresent(key);
    if (cached != null) {
      log.debug(
          "Cache hit for client ID: {} and tenant ID: {}",
          clientId.getValue(),
          tenantId.getValue());
      return Optional.of(cached);
    }

    log.debug(
        "Cache miss for client ID: {} and tenant ID: {}", clientId.getValue(), tenantId.getValue());
    return Optional.empty();
  }

  /**
   * Retrieves a cached client by ID, ignoring tenant.
   *
   * <p>This scans the cache keys for any entry whose suffix matches the client ID.
   *
   * @param clientId client identifier
   * @return first matching client if found; otherwise empty
   */
  @Override
  public Optional<Client> getAnyTenant(ClientId clientId) {
    if (clientId == null) return Optional.empty();

    var clientIdStr = clientId.getValue().toString();
    var result =
        cache.asMap().entrySet().stream()
            .filter(entry -> entry.getKey().endsWith(CACHE_KEY_SEPARATOR + clientIdStr))
            .map(entry -> entry.getValue())
            .findFirst();

    if (result.isPresent())
      log.debug("Cache hit for client ID: {} (any tenant search)", clientId.getValue());
    else log.debug("Cache miss for client ID: {} (any tenant search)", clientId.getValue());

    return result;
  }

  /**
   * Evicts a single client from the cache for the given tenant.
   *
   * @param clientId client identifier
   * @param tenantId tenant identifier
   */
  @Override
  public void evict(ClientId clientId, TenantId tenantId) {
    if (clientId == null || tenantId == null) {
      log.warn("Attempted to evict client with null ID or tenant ID");
      return;
    }

    var key = buildCacheKey(tenantId, clientId);
    cache.invalidate(key);
    log.debug(
        "Evicted client from cache with ID: {} for tenant: {}",
        clientId.getValue(),
        tenantId.getValue());
  }

  /**
   * Evicts all cached clients for the given tenant.
   *
   * @param tenantId tenant identifier
   */
  @Override
  public void evictAllByTenantId(TenantId tenantId) {
    if (tenantId == null) {
      log.warn("Attempted to evict clients with null tenant ID");
      return;
    }

    var prefix = tenantId.getValue() + CACHE_KEY_SEPARATOR;
    var keysToRemove =
        cache.asMap().keySet().stream().filter(key -> key.startsWith(prefix)).toList();

    keysToRemove.forEach(cache::invalidate);
    log.debug(
        "Evicted {} client(s) from cache for tenant ID: {}",
        keysToRemove.size(),
        tenantId.getValue());
  }

  /** Clears the entire client cache. */
  @Override
  public void clear() {
    cache.invalidateAll();
    log.debug("Cleared entire client cache");
  }
}
