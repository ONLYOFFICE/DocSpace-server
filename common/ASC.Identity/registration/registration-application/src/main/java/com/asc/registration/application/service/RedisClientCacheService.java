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

package com.asc.registration.application.service;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.utilities.cache.CacheNamespaceRegistry;
import com.asc.registration.application.transfer.CachedClient;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import java.time.Duration;
import java.util.Objects;
import java.util.Optional;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;

/**
 * Implementation of {@link ClientCacheService} using Redis as distributed cache.
 *
 * <p>The cache is automatically populated and evicted based on domain events processed
 * transactionally, ensuring consistency across multiple application instances.
 *
 * <p>Entries are keyed by client id alone and carry the namespace version of their tenant, so
 * evicting a tenant is a single counter increment rather than a search for the keys that belong to
 * it. Keying by client id keeps one entry per client regardless of how often its tenant is evicted,
 * and lets {@link #getAnyTenant(ClientId)} read an entry without knowing the tenant up front: the
 * tenant comes out of the snapshot itself. No operation scans the keyspace.
 *
 * <p>This service is only loaded when Redis classes are available on the classpath.
 *
 * @see ClientCacheService
 * @see CacheNamespaceRegistry
 * @see Client
 */
@Slf4j
@Service
@ConditionalOnClass(RedisTemplate.class)
public class RedisClientCacheService implements ClientCacheService {
  private static final String CACHE_KEY_SEPARATOR = ":";
  private static final String CACHE_PREFIX = "identity:registration:client";

  private static final int CACHE_EXPIRE_AFTER_WRITE_MINUTES = 3;

  private final CacheNamespaceRegistry versionRegistry;
  private final RedisTemplate<String, Object> redisTemplate;

  /**
   * Constructs a new RedisClientCacheService.
   *
   * @param versionRegistry The registry resolving per-tenant namespace versions.
   * @param redisTemplate The Redis template for cache entries.
   */
  public RedisClientCacheService(
      CacheNamespaceRegistry versionRegistry,
      @Qualifier("clientCacheRedisTemplate") RedisTemplate<String, Object> redisTemplate) {
    this.versionRegistry = versionRegistry;
    this.redisTemplate = redisTemplate;
  }

  /**
   * Builds a cache key for a client.
   *
   * @param clientId The client ID to build the cache key from.
   * @return The cache key in format: identity:registration:client:{clientId}
   */
  private String buildCacheKey(String clientId) {
    return CACHE_PREFIX + CACHE_KEY_SEPARATOR + clientId;
  }

  /**
   * Returns the ID of the tenant owning a client, or {@code null} when the snapshot carries no
   * tenant information.
   *
   * @param client The client to read the tenant ID from.
   * @return The tenant ID, or {@code null} if absent.
   */
  private Long extractTenantId(Client client) {
    if (client == null || client.getClientTenantInfo() == null) return null;

    var tenantId = client.getClientTenantInfo().tenantId();
    return tenantId == null ? null : tenantId.getValue();
  }

  /**
   * Tells whether a cached snapshot may still be served.
   *
   * @param cached The snapshot read from Redis.
   * @param tenantId The tenant owning the cached client.
   * @param requiredTenantId The tenant the caller asked for, or {@code null} if any tenant will do.
   * @return {@code true} if the snapshot belongs to the requested tenant and its namespace is still
   *     current.
   */
  private boolean isCurrent(CachedClient cached, long tenantId, Long requiredTenantId) {
    if (requiredTenantId != null && requiredTenantId != tenantId) return false;

    return Objects.equals(cached.getCacheNamespace(), versionRegistry.namespaceOf(tenantId));
  }

  /**
   * Reads a cached client, accepting it only while the namespace it was stamped with is still
   * current.
   *
   * <p>If deserialization fails, the corrupted entry is removed from cache and an empty Optional is
   * returned.
   *
   * @param clientId The ID of the client to read.
   * @param requiredTenantId The tenant the entry must belong to, or {@code null} to accept the
   *     entry whichever tenant owns it.
   * @return An Optional containing the client if found and still current, empty otherwise.
   */
  private Optional<Client> read(String clientId, Long requiredTenantId) {
    var key = buildCacheKey(clientId);
    try {
      if (redisTemplate.opsForValue().get(key) instanceof CachedClient cached) {
        var client = cached.getClient();
        var tenantId = extractTenantId(client);
        if (tenantId != null && isCurrent(cached, tenantId, requiredTenantId)) {
          log.info("Cache hit for client ID: {} and tenant ID: {}", clientId, tenantId);
          return Optional.of(client);
        }
      }
    } catch (Exception e) {
      log.error("Failed to retrieve client from Redis cache: {}", clientId, e);
      try {
        redisTemplate.delete(key);
      } catch (Exception dex) {
        log.error("Failed to delete corrupted cache entry for client ID: {}", clientId, dex);
      }
    }

    log.info("Cache miss for client ID: {}", clientId);
    return Optional.empty();
  }

  /**
   * Stores a client in Redis cache, stamped with the current namespace of its tenant.
   *
   * <p>If serialization fails, the error is logged. If the client, its ID or its tenant information
   * is null, the operation is skipped with a warning.
   *
   * @param client The client entity to cache. Must not be null and must have a non-null ID.
   */
  @Override
  public void put(Client client) {
    if (client == null || client.getId() == null) {
      log.warn("Attempted to cache null client or client with null ID");
      return;
    }

    var tenantId = extractTenantId(client);
    if (tenantId == null) {
      log.warn(
          "Attempted to cache client without tenant information: {}", client.getId().getValue());
      return;
    }

    var clientId = client.getId().getValue().toString();
    var namespace = versionRegistry.namespaceOf(tenantId);
    if (namespace == null) {
      log.warn(
          "Skipped caching client with ID: {} because its namespace could not be resolved",
          clientId);
      return;
    }

    var key = buildCacheKey(clientId);
    try {
      redisTemplate
          .opsForValue()
          .set(
              key,
              new CachedClient(namespace, client),
              Duration.ofMinutes(CACHE_EXPIRE_AFTER_WRITE_MINUTES));

      log.info("Cached client with ID: {}, Redis key: {}", clientId, key);
    } catch (Exception e) {
      log.error("Failed to cache client in Redis: {}", clientId, e);
    }
  }

  /**
   * Retrieves a client from the cache by client ID, provided it belongs to the given tenant.
   *
   * @param clientId The ID of the client to retrieve. If null, returns empty Optional.
   * @param tenantId The tenant the client must belong to. If null, returns empty Optional.
   * @return An Optional containing the client if found, or empty if not found or parameters are
   *     null.
   */
  @Override
  public Optional<Client> get(ClientId clientId, TenantId tenantId) {
    if (clientId == null || tenantId == null) return Optional.empty();

    return read(clientId.getValue().toString(), tenantId.getValue());
  }

  /**
   * Retrieves a client from the cache by client ID only, taking its tenant from the cached
   * snapshot.
   *
   * <p>Useful when tenant context is not available but cache lookup is still desired.
   *
   * @param clientId The ID of the client to retrieve. If null, returns empty Optional.
   * @return An Optional containing the matching client if found, or empty if not found.
   */
  @Override
  public Optional<Client> getAnyTenant(ClientId clientId) {
    if (clientId == null) return Optional.empty();

    return read(clientId.getValue().toString(), null);
  }

  /**
   * Removes a client from Redis cache.
   *
   * @param clientId The ID of the client to evict from cache. If null, no operation is performed.
   * @param tenantId The tenant owning the client. If null, no operation is performed.
   */
  @Override
  public void evict(ClientId clientId, TenantId tenantId) {
    if (clientId == null || tenantId == null) {
      log.warn("Attempted to evict client with null ID or tenant ID");
      return;
    }

    var id = clientId.getValue().toString();
    try {
      redisTemplate.unlink(buildCacheKey(id));
      log.info("Evicted client from cache with ID: {} for tenant: {}", id, tenantId.getValue());
    } catch (Exception e) {
      log.error("Failed to evict client from cache: {}", id, e);
    }
  }

  /**
   * Removes all clients belonging to a specific tenant from Redis cache.
   *
   * <p>Advances the tenant's namespace version, which detaches every entry cached under the
   * previous version in constant time. The detached entries stop being served immediately and are
   * replaced in place by the next write, or reclaimed by their own TTL if no write comes.
   *
   * @param tenantId The tenant ID whose clients should be evicted. If null, no operation is
   *     performed.
   */
  @Override
  public void evictAllByTenantId(TenantId tenantId) {
    if (tenantId == null) {
      log.warn("Attempted to evict clients with null tenant ID");
      return;
    }

    versionRegistry.invalidateTenant(tenantId.getValue());
    log.info("Evicted all cached clients for tenant ID: {}", tenantId.getValue());
  }

  /**
   * Clears the entire client cache.
   *
   * <p>Advances the global namespace, which detaches every cached entry at once regardless of how
   * many there are. The detached entries are reclaimed by their own TTL.
   */
  @Override
  public void clear() {
    versionRegistry.invalidateAll();
    log.info("Cleared entire client cache");
  }
}
