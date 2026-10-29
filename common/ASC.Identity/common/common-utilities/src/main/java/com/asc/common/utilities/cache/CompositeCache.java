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

package com.asc.common.utilities.cache;

import com.asc.common.utilities.cache.bus.CacheInvalidationBus;
import java.time.Duration;
import java.util.Optional;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.redis.core.RedisTemplate;

/**
 * Two-tier cache: an {@link MemoryCache} in every application instance in front of Redis, which the
 * instances of a region share.
 *
 * <p>Entries are keyed by id alone and stamped with the {@link CacheNamespaceRegistry} namespace of
 * their tenant, so a lookup needs no tenant up front, and evicting a tenant or the whole cache is a
 * single counter increment rather than a search for the keys involved. No operation scans the
 * keyspace. Evicting one entry removes it from Redis and broadcasts the eviction so that every
 * instance drops its own copy.
 *
 * <p>Reads try the instance tier first and fall back to Redis, promoting what they find there.
 * Writes go to both tiers. The instance tier expires far sooner than Redis, which bounds how long
 * an instance that missed a broadcast can keep serving a stale value.
 *
 * <p>Subclasses decide which tenant owns a value and how it is stored in Redis.
 *
 * @param <V> the type of cached values; they are shared between callers and must be treated as
 *     read-only once cached.
 */
@Slf4j
public abstract class CompositeCache<V> {
  private static final String KEY_SEPARATOR = ":";
  private static final int MEMORY_MAX_SIZE = 10_000;
  private static final Duration REDIS_EXPIRE_AFTER_WRITE = Duration.ofMinutes(3);
  private static final Duration MEMORY_EXPIRE_AFTER_WRITE = Duration.ofSeconds(10);

  /**
   * A value read back from Redis together with the namespace it was written under.
   *
   * @param namespace the namespace the value was stamped with.
   * @param value the cached value.
   * @param <V> the type of the cached value.
   */
  public record Stamped<V>(String namespace, V value) {}

  private final String keyPrefix;
  private final MemoryCache<V> memoryCache;
  private final CacheNamespaceRegistry registry;
  private final RedisTemplate<String, Object> redisTemplate;

  /**
   * @param keyPrefix the prefix of this cache's Redis keys.
   * @param bus the bus broadcasting evictions to the instance tier of every instance.
   * @param registry the registry resolving per-tenant namespaces.
   * @param redisTemplate the Redis template storing the entries.
   */
  protected CompositeCache(
      String keyPrefix,
      CacheInvalidationBus bus,
      CacheNamespaceRegistry registry,
      RedisTemplate<String, Object> redisTemplate) {
    this.keyPrefix = keyPrefix;
    this.redisTemplate = redisTemplate;
    this.registry = registry;
    this.memoryCache = new MemoryCache<>(registry, bus, MEMORY_MAX_SIZE, MEMORY_EXPIRE_AFTER_WRITE);
  }

  /**
   * Returns the tenant owning a value.
   *
   * @param value the cached value.
   * @return the tenant ID, or {@code null} if the value carries no tenant; such values are never
   *     cached.
   */
  protected abstract Long tenantOf(V value);

  /**
   * Builds the object stored in Redis for a value.
   *
   * @param namespace the namespace to stamp the value with.
   * @param value the value to store.
   * @return the object to write to Redis.
   */
  protected abstract Object toStored(String namespace, V value);

  /**
   * Reads a value back from what is stored in Redis.
   *
   * @param stored the object read from Redis, possibly {@code null}.
   * @return the stamped value, or {@code null} if the object is not one this cache wrote.
   */
  protected abstract Stamped<V> fromStored(Object stored);

  private String buildCacheKey(String id) {
    return keyPrefix + KEY_SEPARATOR + id;
  }

  /**
   * Reads a value from the instance tier, then from Redis, accepting it only while the namespace it
   * was stamped with is still current. A value found in Redis is promoted to the instance tier.
   *
   * <p>If reading Redis fails, the entry is assumed corrupted and removed.
   *
   * @param id the id of the value.
   * @return the value, or empty if absent or detached by a namespace advance.
   */
  protected Optional<V> read(String id) {
    var key = buildCacheKey(id);
    var cached = memoryCache.get(key);
    if (cached.isPresent()) {
      log.debug("Memory cache hit for key: {}", key);
      return cached;
    }

    var generation = memoryCache.generation();
    try {
      var stored = fromStored(redisTemplate.opsForValue().get(key));
      var tenantId = stored == null ? null : tenantOf(stored.value());
      if (tenantId != null && registry.isCurrent(tenantId, stored.namespace())) {
        log.info("Cache hit for key: {}", key);
        memoryCache.promote(key, tenantId, stored.namespace(), stored.value(), generation);
        return Optional.of(stored.value());
      }
    } catch (Exception e) {
      log.error("Failed to read cache entry: {}", key, e);
      try {
        redisTemplate.delete(key);
      } catch (Exception dex) {
        log.error("Failed to delete corrupted cache entry: {}", key, dex);
      }
    }

    log.info("Cache miss for key: {}", key);
    return Optional.empty();
  }

  /**
   * Stores a value in both tiers, stamped with the current namespace of its tenant. Values without
   * a tenant, or whose namespace cannot be resolved, are skipped.
   *
   * @param id the id of the value.
   * @param value the value to cache.
   */
  protected void write(String id, V value) {
    var key = buildCacheKey(id);
    var tenantId = tenantOf(value);
    if (tenantId == null) {
      log.warn("Skipped caching key: {} because it has no tenant", key);
      return;
    }

    var namespace = registry.namespaceOf(tenantId);
    if (namespace == null) {
      log.warn("Skipped caching key: {} because its namespace could not be resolved", key);
      return;
    }

    try {
      redisTemplate.opsForValue().set(key, toStored(namespace, value), REDIS_EXPIRE_AFTER_WRITE);
      memoryCache.put(key, tenantId, namespace, value);
      log.info("Cached key: {}", key);
    } catch (Exception e) {
      log.error("Failed to cache key: {}", key, e);
    }
  }

  /**
   * Removes a value from Redis and from the instance tier of every instance.
   *
   * @param id the id of the value.
   */
  protected void remove(String id) {
    var key = buildCacheKey(id);
    try {
      redisTemplate.unlink(key);
      log.info("Evicted key: {}", key);
    } catch (Exception e) {
      log.error("Failed to evict key: {}", key, e);
    }

    memoryCache.invalidate(key);
  }

  /**
   * Detaches every value of a tenant by advancing its namespace. Detached Redis entries are
   * replaced by the next write or reclaimed by their own TTL.
   *
   * @param tenantId the tenant whose values should be evicted.
   */
  protected void removeTenant(long tenantId) {
    memoryCache.invalidateTenant(tenantId);
    log.info("Evicted all cached entries of tenant: {} under prefix: {}", tenantId, keyPrefix);
  }

  /** Detaches every value by advancing the global namespace. */
  protected void removeAll() {
    memoryCache.invalidateAll();
    log.info("Evicted all cached entries under prefix: {}", keyPrefix);
  }
}
