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

import com.asc.authorization.data.client.cache.CachedRegisteredClient;
import com.asc.authorization.data.client.cache.RegisteredClientCacheService;
import com.asc.common.utilities.cache.CacheNamespaceRegistry;
import com.asc.common.utilities.cache.CompositeCache;
import com.asc.common.utilities.cache.bus.CacheInvalidationBus;
import java.util.Optional;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Profile;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;

/**
 * {@link RegisteredClientCacheService} on a {@link CompositeCache}: an in-process tier in front of
 * Redis.
 *
 * <p>Entries are keyed by client id alone, since lookups happen before a tenant is known; the
 * snapshot itself carries its tenant and the namespace it was written under.
 *
 * @see CompositeCache
 */
@Slf4j
@Service
@Profile("saas")
@ConditionalOnClass(RedisTemplate.class)
public class CachingRegisteredClientService extends CompositeCache<CachedRegisteredClient>
    implements RegisteredClientCacheService {
  private static final String CACHE_PREFIX = "identity:authorization:client";

  public CachingRegisteredClientService(
      @Qualifier("registeredClientCacheRedisTemplate") RedisTemplate<String, Object> redisTemplate,
      CacheNamespaceRegistry versionRegistry,
      CacheInvalidationBus invalidationBus) {
    super(CACHE_PREFIX, invalidationBus, versionRegistry, redisTemplate);
  }

  @Override
  protected Long tenantOf(CachedRegisteredClient client) {
    return client.getTenantId();
  }

  @Override
  protected Object toStored(String namespace, CachedRegisteredClient client) {
    client.setCacheNamespace(namespace);
    return client;
  }

  @Override
  protected Stamped<CachedRegisteredClient> fromStored(Object stored) {
    return stored instanceof CachedRegisteredClient client
        ? new Stamped<>(client.getCacheNamespace(), client)
        : null;
  }

  @Override
  public void put(CachedRegisteredClient client) {
    if (client == null || client.getClientId() == null) {
      log.warn("Attempted to cache null registered client or client with null ID");
      return;
    }

    write(client.getClientId(), client);
  }

  @Override
  public Optional<CachedRegisteredClient> get(String clientId) {
    if (clientId == null || clientId.isEmpty()) return Optional.empty();

    return read(clientId);
  }

  @Override
  public void evict(String clientId, long tenantId) {
    if (clientId == null || clientId.isEmpty()) {
      log.warn("Attempted to evict registered client with null or empty id");
      return;
    }

    remove(clientId);
  }

  @Override
  public void evictAllByTenantId(long tenantId) {
    removeTenant(tenantId);
  }

  @Override
  public void clear() {
    removeAll();
  }
}
