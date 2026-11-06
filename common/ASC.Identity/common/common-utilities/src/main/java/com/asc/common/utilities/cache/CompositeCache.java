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
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.List;
import java.util.Optional;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.data.redis.core.script.RedisScript;
import org.springframework.data.redis.serializer.GenericToStringSerializer;
import org.springframework.data.redis.serializer.RedisSerializer;

/**
 * Two-tier cache: an {@link MemoryCache} in every application instance in front of Redis, which the
 * instances of a region share.
 *
 * <p>Entries are keyed by id alone and stamped with the {@link CacheNamespaceRegistry} namespace of
 * their tenant, so a lookup needs no tenant up front, and evicting a tenant or the whole cache is a
 * single counter increment rather than a search for the keys involved. No operation scans the
 * keyspace. Evicting one entry removes it from Redis, moves its version on, and broadcasts the
 * eviction so that every instance drops its own copy. The version, kept next to the entry, lets a
 * value read from the source after a cache miss be stored only if no eviction or write came first
 * (see {@link #writeUnlessInvalidated}). Evicting a tenant or the whole cache also moves a counter
 * of such evictions on, which a fill checks the same way: it cannot tell which tenant it reads
 * until it has read, so any of them in between stops it.
 *
 * <p>Reads try the instance tier first and fall back to Redis, promoting what they find there.
 * Writes go to both tiers. The instance tier expires far sooner than Redis, which bounds how long
 * an instance that missed a broadcast can keep serving a stale value. An entry whose eviction was
 * lost is served for at most the Redis lifetime plus the instance tier's, as an instance may have
 * promoted it just before it expired in Redis.
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
  private static final Duration DEFAULT_REDIS_EXPIRE_AFTER_WRITE = Duration.ofMinutes(3);
  private static final Duration MEMORY_EXPIRE_AFTER_WRITE = Duration.ofSeconds(10);
  private static final Duration MIN_VERSION_EXPIRE = Duration.ofHours(1);

  private static final String VERSION_SUFFIX = ":version";
  private static final String EVICTIONS_SUFFIX = "::evictions";
  private static final RedisSerializer<Long> VERSION_SERIALIZER =
      new GenericToStringSerializer<>(Long.class);

  @SuppressWarnings("rawtypes")
  static final RedisScript<List> READ_VERSION =
      RedisScript.of(
          """
          return {tonumber(redis.call('GET', KEYS[2]) or '0'),
                  tonumber(redis.call('GET', KEYS[3]) or '0')}""",
          List.class);

  static final RedisScript<Long> EVICT =
      RedisScript.of(
          """
          local version = redis.call('INCR', KEYS[2])
          redis.call('PEXPIRE', KEYS[2], ARGV[1])
          redis.call('UNLINK', KEYS[1])
          return version""",
          Long.class);

  static final RedisScript<Long> EVICT_MANY =
      RedisScript.of(
          """
          local evictions = redis.call('INCR', KEYS[3])
          redis.call('PEXPIRE', KEYS[3], ARGV[1])
          return evictions""",
          Long.class);

  static final RedisScript<Long> WRITE =
      RedisScript.of(
          """
          local version = redis.call('INCR', KEYS[2])
          redis.call('PEXPIRE', KEYS[2], ARGV[3])
          redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2])
          return version""",
          Long.class);

  static final RedisScript<Long> WRITE_IF_VERSION =
      RedisScript.of(
          """
          if (redis.call('GET', KEYS[2]) or '0') ~= ARGV[3] then return 0 end
          if (redis.call('GET', KEYS[3]) or '0') ~= ARGV[4] then return 0 end
          redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2])
          return 1""",
          Long.class);

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
  private final Duration redisExpireAfterWrite;

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
    this(keyPrefix, bus, registry, redisTemplate, DEFAULT_REDIS_EXPIRE_AFTER_WRITE);
  }

  /**
   * @param keyPrefix the prefix of this cache's Redis keys.
   * @param bus the bus broadcasting evictions to the instance tier of every instance.
   * @param registry the registry resolving per-tenant namespaces.
   * @param redisTemplate the Redis template storing the entries.
   * @param redisExpireAfterWrite how long a Redis entry lives after it was written; it bounds how
   *     long a value whose eviction was lost can be served. It must be positive; the instance tier
   *     keeps entries for no longer than this either.
   * @throws IllegalArgumentException if {@code redisExpireAfterWrite} is not positive
   */
  protected CompositeCache(
      String keyPrefix,
      CacheInvalidationBus bus,
      CacheNamespaceRegistry registry,
      RedisTemplate<String, Object> redisTemplate,
      Duration redisExpireAfterWrite) {
    if (redisExpireAfterWrite == null
        || redisExpireAfterWrite.isZero()
        || redisExpireAfterWrite.isNegative())
      throw new IllegalArgumentException(
          "The cache under %s needs a positive lifetime, got %s"
              .formatted(keyPrefix, redisExpireAfterWrite));

    this.keyPrefix = keyPrefix;
    this.redisTemplate = redisTemplate;
    this.redisExpireAfterWrite = redisExpireAfterWrite;
    this.registry = registry;
    var memoryExpireAfterWrite =
        redisExpireAfterWrite.compareTo(MEMORY_EXPIRE_AFTER_WRITE) < 0
            ? redisExpireAfterWrite
            : MEMORY_EXPIRE_AFTER_WRITE;
    this.memoryCache = new MemoryCache<>(registry, bus, MEMORY_MAX_SIZE, memoryExpireAfterWrite);
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

  private String ttlMillis() {
    return String.valueOf(redisExpireAfterWrite.toMillis());
  }

  private String versionTtlMillis() {
    return String.valueOf(
        Math.max(redisExpireAfterWrite.toMillis(), MIN_VERSION_EXPIRE.toMillis()));
  }

  private static byte[] raw(Object arg) {
    return arg instanceof byte[] bytes
        ? bytes
        : String.valueOf(arg).getBytes(StandardCharsets.UTF_8);
  }

  /** Runs a script over the entry of {@code id}, its version and the eviction counter. */
  @SuppressWarnings({"unchecked", "rawtypes"})
  private <T> T runScript(RedisScript<T> script, String id, Object... args) {
    var rawArgs = new Object[args.length];
    for (var i = 0; i < args.length; i++) rawArgs[i] = raw(args[i]);

    return (T)
        redisTemplate.execute(
            (RedisScript) script,
            RedisSerializer.byteArray(),
            (RedisSerializer) VERSION_SERIALIZER,
            List.of(
                buildCacheKey(id),
                buildCacheKey(id) + VERSION_SUFFIX,
                keyPrefix + EVICTIONS_SUFFIX),
            rawArgs);
  }

  /**
   * Moves the eviction counter on, so that fills already under way do not store what they read.
   * Fills on every instance of the region see it, whether or not the eviction broadcast reaches
   * them.
   */
  private void countEviction() {
    try {
      runScript(EVICT_MANY, "", versionTtlMillis());
    } catch (Exception e) {
      log.error("Failed to count an eviction under prefix: {}", keyPrefix, e);
    }
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
    store(id, value, null);
  }

  protected void writeUnlessInvalidated(String id, V value, CacheTicket ticket) {
    store(id, value, ticket);
  }

  private void store(String id, V value, CacheTicket ticket) {
    var key = buildCacheKey(id);
    if (ticket != null
        && (ticket.version() == null
            || ticket.evictions() == null
            || memoryCache.generation() != ticket.generation())) {
      log.info("Skipped caching key: {} because it was invalidated while being read", key);
      return;
    }

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
      @SuppressWarnings("unchecked")
      var serializer = (RedisSerializer<Object>) redisTemplate.getValueSerializer();
      var stored = serializer.serialize(toStored(namespace, value));
      if (ticket == null) {
        runScript(WRITE, id, stored, ttlMillis(), versionTtlMillis());
        memoryCache.put(key, tenantId, namespace, value);
      } else {
        Long written =
            runScript(
                WRITE_IF_VERSION, id, stored, ttlMillis(), ticket.version(), ticket.evictions());
        if (written == null || written == 0) {
          log.info("Skipped caching key: {} because it was invalidated while being read", key);
          return;
        }

        memoryCache.promote(key, tenantId, namespace, value, ticket.generation());
      }

      log.info("Cached key: {}", key);
    } catch (Exception e) {
      log.error("Failed to cache key: {}", key, e);
    }
  }

  /**
   * Removes a value from Redis, moves its version on, and drops it from the instance tier of every
   * instance.
   *
   * @param id the id of the value.
   */
  protected void remove(String id) {
    var key = buildCacheKey(id);
    try {
      runScript(EVICT, id, versionTtlMillis());
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
    countEviction();
    memoryCache.invalidateTenant(tenantId);
    log.info("Evicted all cached entries of tenant: {} under prefix: {}", tenantId, keyPrefix);
  }

  /** Detaches every value by advancing the global namespace. */
  protected void removeAll() {
    countEviction();
    memoryCache.invalidateAll();
    log.info("Evicted all cached entries under prefix: {}", keyPrefix);
  }

  /**
   * Takes the ticket a cache-miss fill hands to {@link #writeUnlessInvalidated}: the invalidation
   * generation of this instance, the entry's version in Redis and the eviction counter. Take it
   * before reading the value from its source.
   *
   * @param id the id of the value about to be read.
   * @return the ticket; its version is {@code null} if Redis could not be read, and such a ticket
   *     caches nothing.
   */
  public CacheTicket ticket(String id) {
    var generation = memoryCache.generation();
    try {
      List<?> versions = runScript(READ_VERSION, id);
      return new CacheTicket(
          generation,
          ((Number) versions.get(0)).longValue(),
          ((Number) versions.get(1)).longValue());
    } catch (Exception e) {
      log.warn("Failed to read the cache version of key: {}", buildCacheKey(id), e);
      return new CacheTicket(generation, null);
    }
  }
}
