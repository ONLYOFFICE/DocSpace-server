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
import java.time.Duration;
import java.util.HashSet;
import java.util.Optional;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Profile;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;

/** Redis-backed implementation of {@link RegisteredClientCacheService}. */
@Slf4j
@Service
@Profile("saas")
@ConditionalOnClass(RedisTemplate.class)
public class CachingRegisteredClientService implements RegisteredClientCacheService {
  private static final String CACHE_PREFIX = "identity:authorization:client";
  private static final String CACHE_KEY_TENANT_CLIENT_SEPARATOR = "_";
  private static final String CACHE_KEY_SEPARATOR = ":";

  private static final int CACHE_EXPIRE_AFTER_WRITE_MINUTES = 5;

  private final RedisTemplate<String, Object> redisTemplate;

  public CachingRegisteredClientService(
      @Qualifier("registeredClientCacheRedisTemplate")
          RedisTemplate<String, Object> redisTemplate) {
    this.redisTemplate = redisTemplate;
  }

  private String buildCacheKey(long tenantId, String clientId) {
    return CACHE_PREFIX
        + CACHE_KEY_SEPARATOR
        + tenantId
        + CACHE_KEY_TENANT_CLIENT_SEPARATOR
        + clientId;
  }

  private String buildTenantCacheKeyPattern(long tenantId) {
    return CACHE_PREFIX + CACHE_KEY_SEPARATOR + tenantId + CACHE_KEY_TENANT_CLIENT_SEPARATOR + "*";
  }

  private String buildAnyTenantCacheKeyPattern(String clientId) {
    return CACHE_PREFIX + CACHE_KEY_SEPARATOR + "*" + CACHE_KEY_TENANT_CLIENT_SEPARATOR + clientId;
  }

  @Override
  public void put(CachedRegisteredClient client) {
    if (client == null || client.getClientId() == null) {
      log.warn("Attempted to cache null registered client or client with null ID");
      return;
    }

    var key = buildCacheKey(client.getTenantId(), client.getClientId());
    try {
      redisTemplate
          .opsForValue()
          .set(key, client, Duration.ofMinutes(CACHE_EXPIRE_AFTER_WRITE_MINUTES));

      log.info("Cached registered client with key {}", key);
    } catch (Exception e) {
      log.error("Failed to cache registered client in Redis", e);
    }
  }

  @Override
  public Optional<CachedRegisteredClient> get(String clientId) {
    if (clientId == null || clientId.isEmpty()) return Optional.empty();

    try {
      var pattern = buildAnyTenantCacheKeyPattern(clientId);
      var keys = redisTemplate.keys(pattern);

      if (keys != null && !keys.isEmpty()) {
        for (var key : keys) {
          try {
            var cached = redisTemplate.opsForValue().get(key);
            if (cached instanceof CachedRegisteredClient client) {
              log.info("Cache hit for registered client");
              return Optional.of(client);
            }

          } catch (Exception e) {
            log.info("Failed to retrieve cached entry for key {}", key);
            try {
              redisTemplate.delete(key);
            } catch (Exception dex) {
              log.error("Failed to delete corrupted cache entry", dex);
            }
          }
        }
      }
    } catch (Exception e) {
      log.error("Failed to search cache for registered client", e);
    }

    log.info("Cache miss for registered client");

    return Optional.empty();
  }

  @Override
  public void evict(String clientId, long tenantId) {
    if (clientId == null || clientId.isEmpty()) {
      log.warn("Attempted to evict registered client with null or empty id");
      return;
    }

    var key = buildCacheKey(tenantId, clientId);
    try {
      redisTemplate.delete(key);
    } catch (Exception e) {
      log.error("Failed to evict registered client from cache", e);
    }
  }

  @Override
  public void evictAllByTenantId(long tenantId) {
    try {
      var pattern = buildTenantCacheKeyPattern(tenantId);
      var keys = redisTemplate.keys(pattern);
      if (keys != null && !keys.isEmpty()) redisTemplate.delete(keys);
    } catch (Exception e) {
      log.error("Failed to evict registered clients for tenant", e);
    }
  }

  @Override
  public void clear() {
    try {
      var keys = redisTemplate.keys(CACHE_PREFIX + CACHE_KEY_SEPARATOR + "*");
      if (keys != null && !keys.isEmpty()) redisTemplate.delete(new HashSet<>(keys));
    } catch (Exception e) {
      log.error("Failed to clear registered client cache", e);
    }
  }
}
