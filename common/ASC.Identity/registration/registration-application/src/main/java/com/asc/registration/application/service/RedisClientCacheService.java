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
import com.asc.common.utilities.cache.CacheTicket;
import com.asc.common.utilities.cache.CompositeCache;
import com.asc.common.utilities.cache.bus.CacheInvalidationBus;
import com.asc.registration.application.transfer.CachedClient;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import java.util.Optional;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;

/**
 * {@link ClientCacheService} on a {@link CompositeCache}: an in-process tier in front of Redis.
 *
 * <p>The cache is automatically populated and evicted based on domain events processed
 * transactionally, ensuring consistency across multiple application instances. Entries are keyed by
 * client id alone, which lets {@link #getAnyTenant(ClientId)} read an entry without knowing the
 * tenant up front: the tenant comes out of the snapshot itself.
 *
 * <p>This service is only loaded when Redis classes are available on the classpath and {@code
 * client.cache.redis.enabled=true}, the same switch that enables {@link
 * com.asc.registration.application.configuration.ClientCacheConfiguration}, which provides its
 * Redis beans. Otherwise {@link NoOpClientCacheService} takes its place.
 *
 * @see ClientCacheService
 * @see CompositeCache
 * @see Client
 */
@Slf4j
@Service
@ConditionalOnClass(RedisTemplate.class)
@ConditionalOnProperty(prefix = "client.cache.redis", name = "enabled", havingValue = "true")
public class RedisClientCacheService extends CompositeCache<Client> implements ClientCacheService {
  private static final String CACHE_PREFIX = "identity:registration:client";

  /**
   * Constructs a new RedisClientCacheService.
   *
   * @param versionRegistry The registry resolving per-tenant namespace versions.
   * @param redisTemplate The Redis template for cache entries.
   * @param invalidationBus The bus broadcasting evictions to the in-process tier of every instance.
   */
  public RedisClientCacheService(
      CacheNamespaceRegistry versionRegistry,
      @Qualifier("clientCacheRedisTemplate") RedisTemplate<String, Object> redisTemplate,
      CacheInvalidationBus invalidationBus) {
    super(CACHE_PREFIX, invalidationBus, versionRegistry, redisTemplate);
  }

  @Override
  protected Long tenantOf(Client client) {
    if (client == null || client.getClientTenantInfo() == null) return null;

    var tenantId = client.getClientTenantInfo().tenantId();
    return tenantId == null ? null : tenantId.getValue();
  }

  @Override
  protected Object toStored(String namespace, Client client) {
    return new CachedClient(namespace, client);
  }

  @Override
  protected Stamped<Client> fromStored(Object stored) {
    return stored instanceof CachedClient cached
        ? new Stamped<>(cached.getCacheNamespace(), cached.getClient())
        : null;
  }

  /**
   * Stores a client in both tiers, stamped with the current namespace of its tenant.
   *
   * @param client The client entity to cache. Must not be null and must have a non-null ID.
   */
  @Override
  public void put(Client client) {
    if (client == null || client.getId() == null) {
      log.warn("Attempted to cache null client or client with null ID");
      return;
    }

    write(client.getId().getValue().toString(), client);
  }

  @Override
  public CacheTicket ticket(ClientId clientId) {
    return ticket(clientId.getValue().toString());
  }

  /**
   * Stores a client read from the store, unless the cache was invalidated since {@code ticket} was
   * taken.
   *
   * @param client The client read from the store. Must not be null and must have a non-null ID.
   * @param ticket The ticket taken with {@link #ticket(ClientId)} before the store was read.
   */
  @Override
  public void putUnlessInvalidated(Client client, CacheTicket ticket) {
    if (client == null || client.getId() == null) {
      log.warn("Attempted to cache null client or client with null ID");
      return;
    }

    writeUnlessInvalidated(client.getId().getValue().toString(), client, ticket);
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

    return read(clientId.getValue().toString())
        .filter(client -> tenantId.getValue().equals(tenantOf(client)));
  }

  /**
   * Retrieves a client from the cache by client ID only, taking its tenant from the cached
   * snapshot.
   *
   * @param clientId The ID of the client to retrieve. If null, returns empty Optional.
   * @return An Optional containing the matching client if found, or empty if not found.
   */
  @Override
  public Optional<Client> getAnyTenant(ClientId clientId) {
    if (clientId == null) return Optional.empty();

    return read(clientId.getValue().toString());
  }

  /**
   * Removes a client from Redis and from the in-process tier of every instance.
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

    remove(clientId.getValue().toString());
  }

  /**
   * Removes all clients belonging to a specific tenant from the cache.
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

    removeTenant(tenantId.getValue());
  }

  /** Clears the entire client cache. */
  @Override
  public void clear() {
    removeAll();
  }
}
