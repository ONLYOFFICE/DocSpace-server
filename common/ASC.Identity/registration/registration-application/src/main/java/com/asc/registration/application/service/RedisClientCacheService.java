// (c) Copyright Ascensio System SIA 2009-2025
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

package com.asc.registration.application.service;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import java.time.Duration;
import java.util.HashSet;
import java.util.Optional;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;

/**
 * Implementation of {@link ClientCacheService} using Redis as distributed cache.
 *
 * <p>The cache is automatically populated and evicted based on domain events processed
 * transactionally, ensuring consistency across multiple application instances.
 *
 * @see ClientCacheService
 * @see Client
 */
@Slf4j
@Service
public class RedisClientCacheService implements ClientCacheService {
  private static final String CACHE_KEY_SEPARATOR = ":";
  private static final String CACHE_KEY_TENANT_CLIENT_SEPARATOR = "_";
  private static final String CACHE_PREFIX = "identity:registration:client";

  private static final int CACHE_EXPIRE_AFTER_WRITE_MINUTES = 5;

  private final RedisTemplate<String, Object> redisTemplate;

  /**
   * Constructs a new MultiLevelCacheService with Redis cache.
   *
   * @param redisTemplate The Redis template for cache operations.
   */
  public RedisClientCacheService(
      @Qualifier("clientCacheRedisTemplate") RedisTemplate<String, Object> redisTemplate) {
    this.redisTemplate = redisTemplate;
  }

  /**
   * Builds a cache key from a tenant ID and client ID.
   *
   * @param tenantId The tenant ID to build the cache key from.
   * @param clientId The client ID to build the cache key from.
   * @return The cache key string in format: identity:registration:client:{tenantId}::{clientId}
   */
  private String buildCacheKey(TenantId tenantId, ClientId clientId) {
    return CACHE_PREFIX
        + CACHE_KEY_SEPARATOR
        + tenantId.getValue()
        + CACHE_KEY_TENANT_CLIENT_SEPARATOR
        + clientId.getValue().toString();
  }

  /**
   * Builds a cache key pattern for all clients belonging to a tenant.
   *
   * @param tenantId The tenant ID to build the pattern for.
   * @return The cache key pattern string in format: identity:registration:client:{tenantId}::*
   */
  private String buildTenantCacheKeyPattern(TenantId tenantId) {
    return CACHE_PREFIX
        + CACHE_KEY_SEPARATOR
        + tenantId.getValue()
        + CACHE_KEY_TENANT_CLIENT_SEPARATOR
        + "*";
  }

  /**
   * Stores a client in Redis cache.
   *
   * <p>This method performs the following operations:
   *
   * <ol>
   *   <li>Validates that the client and its ID are not null
   *   <li>Serializes the client to JSON and stores it in Redis cache with TTL
   * </ol>
   *
   * <p>If serialization fails, the error is logged. If the client or its ID is null, the operation
   * is skipped with a warning.
   *
   * @param client The client entity to cache. Must not be null and must have a non-null ID.
   */
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
    try {
      redisTemplate
          .opsForValue()
          .set(key, client, Duration.ofMinutes(CACHE_EXPIRE_AFTER_WRITE_MINUTES));

      log.debug("Cached client with ID: {}, Redis key: {}", client.getId().getValue(), key);
    } catch (Exception e) {
      log.error("Failed to cache client in Redis: {}", client.getId().getValue(), e);
    }
  }

  /**
   * Retrieves a client from the cache by client ID and tenant ID.
   *
   * <p>This method performs a direct cache lookup using the composite key (tenant ID + client ID).
   * The tenant ID is now part of the cache key structure, providing natural tenant isolation and
   * improved cache eviction performance.
   *
   * <p>If deserialization fails, the corrupted entry is removed from cache and an empty Optional is
   * returned.
   *
   * @param clientId The ID of the client to retrieve. If null, returns empty Optional.
   * @param tenantId The tenant ID for cache key lookup. If null, returns empty Optional.
   * @return An Optional containing the client if found, or empty if not found or parameters are
   *     null.
   */
  public Optional<Client> get(ClientId clientId, TenantId tenantId) {
    if (clientId == null || tenantId == null) return Optional.empty();

    var key = buildCacheKey(tenantId, clientId);
    try {
      var cached = redisTemplate.opsForValue().get(key);
      if (cached instanceof Client client) {
        log.debug(
            "Cache hit for client ID: {} and tenant ID: {}",
            clientId.getValue(),
            tenantId.getValue());
        return Optional.of(client);
      }
    } catch (Exception e) {
      log.error("Failed to retrieve client from Redis cache: {}", clientId.getValue(), e);
      try {
        redisTemplate.delete(key);
      } catch (Exception dex) {
        log.error(
            "Failed to delete corrupted cache entry for client ID: {}", clientId.getValue(), dex);
      }
    }

    log.debug(
        "Cache miss for client ID: {} and tenant ID: {}", clientId.getValue(), tenantId.getValue());
    return Optional.empty();
  }

  /**
   * Retrieves a client from the cache by client ID only, searching across all tenants.
   *
   * <p>This method performs a pattern-based scan to find a client with the given ID across all
   * tenant partitions. It's useful when tenant context is not available but cache lookup is still
   * desired.
   *
   * @param clientId The ID of the client to retrieve. If null, returns empty Optional.
   * @return An Optional containing the first matching client if found, or empty if not found.
   */
  public Optional<Client> getAnyTenant(ClientId clientId) {
    if (clientId == null) return Optional.empty();

    try {
      var pattern =
          CACHE_PREFIX
              + CACHE_KEY_SEPARATOR
              + "*"
              + CACHE_KEY_TENANT_CLIENT_SEPARATOR
              + clientId.getValue().toString();
      var keys = redisTemplate.keys(pattern);

      if (keys != null && !keys.isEmpty()) {
        for (var key : keys) {
          try {
            var cached = redisTemplate.opsForValue().get(key);
            if (cached instanceof Client client) {
              log.debug("Cache hit for client ID: {}", clientId.getValue());
              return Optional.of(client);
            }
          } catch (Exception e) {
            log.warn("Failed to retrieve cached entry for key: {}", key, e);
          }
        }
      }
    } catch (Exception e) {
      log.error("Failed to search cache for client ID across tenants: {}", clientId.getValue(), e);
    }

    log.debug("Cache miss for client ID: {} (any tenant search)", clientId.getValue());
    return Optional.empty();
  }

  /**
   * Removes a client from Redis cache.
   *
   * @param clientId The ID of the client to evict from cache. If null, no operation is performed.
   * @param tenantId The tenant ID for cache key lookup. If null, no operation is performed.
   */
  public void evict(ClientId clientId, TenantId tenantId) {
    if (clientId == null || tenantId == null) {
      log.warn("Attempted to evict client with null ID or tenant ID");
      return;
    }

    var key = buildCacheKey(tenantId, clientId);
    try {
      redisTemplate.delete(key);
      log.debug(
          "Evicted client from cache with ID: {} for tenant: {}",
          clientId.getValue(),
          tenantId.getValue());
    } catch (Exception e) {
      log.error("Failed to evict client from cache: {}", clientId, e);
    }
  }

  /**
   * Removes all clients belonging to a specific tenant from Redis cache.
   *
   * @param tenantId The tenant ID whose clients should be evicted. If null, no operation is
   *     performed.
   */
  public void evictAllByTenantId(TenantId tenantId) {
    if (tenantId == null) {
      log.warn("Attempted to evict clients with null tenant ID");
      return;
    }

    try {
      var pattern = buildTenantCacheKeyPattern(tenantId);
      var keys = redisTemplate.keys(pattern);
      if (keys != null && !keys.isEmpty()) {
        var deletedCount = redisTemplate.delete(keys);
        log.debug(
            "Evicted {} client(s) from cache for tenant ID: {} using pattern: {}",
            deletedCount,
            tenantId.getValue(),
            pattern);
      } else {
        log.debug("No clients found in cache for tenant ID: {}", tenantId.getValue());
      }
    } catch (Exception e) {
      log.error("Failed to evict clients for tenant ID: {}", tenantId.getValue(), e);
    }
  }

  /**
   * Clears all entries from Redis cache.
   *
   * <p>This method performs a complete cache flush by deleting all client-prefixed keys from Redis.
   *
   * <p>If the clear operation fails (e.g., due to Redis connectivity issues), the error is logged.
   */
  public void clear() {
    try {
      var keys = redisTemplate.keys(CACHE_PREFIX + CACHE_KEY_SEPARATOR + "*");
      if (keys != null && !keys.isEmpty()) {
        var stringKeys = new HashSet<>(keys);
        redisTemplate.delete(stringKeys);
      }

      log.debug("Cleared entire client cache");
    } catch (Exception e) {
      log.error("Failed to clear cache", e);
    }
  }
}
