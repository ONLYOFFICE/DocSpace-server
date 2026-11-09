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
import com.github.benmanes.caffeine.cache.Cache;
import com.github.benmanes.caffeine.cache.Caffeine;
import java.time.Duration;
import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicLong;
import lombok.extern.slf4j.Slf4j;

/**
 * In-process cache of one application instance, placed in front of a cache shared through Redis.
 *
 * <p>Entries are stamped with the {@link CacheNamespaceRegistry} namespace of their tenant, the
 * same stamp the shared tier uses, and are served only while it is still current. Tenant-wide and
 * global evictions therefore need no extra work here: advancing the namespace detaches instance and
 * Redis entries alike, on every instance, within the registry's memoization window.
 *
 * <p>Single-entry evictions do not move a namespace, so they are broadcast over a {@link
 * CacheInvalidationBus} for every instance to drop its own copy. Tenant and global evictions are
 * broadcast too, which makes the other instances drop their memoized namespace at once instead of
 * waiting out the memoization window. A lost message is bounded by this cache's expiry, which
 * should stay well below the shared tier's.
 *
 * <p>Promoting a Redis entry into this cache races with invalidations that arrive while the Redis
 * read is in flight. Callers take a {@link #generation()} token before the Redis read and hand it
 * to {@link #promote}, which refuses to keep an entry if any invalidation happened in between.
 *
 * <p>Values are shared between callers rather than copied, so they must be treated as read-only.
 *
 * @param <V> the type of cached values
 */
@Slf4j
public class MemoryCache<V> {
  private static final String SEPARATOR = "|";
  private static final String KEY = "k";
  private static final String TENANT = "t";
  private static final String ALL = "a";

  private record Entry<V>(long tenantId, String namespace, V value) {}

  private final String origin = UUID.randomUUID().toString();
  private final AtomicLong generation = new AtomicLong();
  private final Cache<String, Entry<V>> entries;
  private final CacheNamespaceRegistry registry;
  private final CacheInvalidationBus bus;

  /**
   * Creates a memory cache and subscribes it to the invalidations of other instances.
   *
   * @param registry the namespace registry shared with the shared tier.
   * @param bus the bus carrying invalidations between instances.
   * @param maximumSize the maximum number of entries kept in memory.
   * @param expireAfterWrite how long an entry may be served at most; bounds lost invalidations.
   */
  public MemoryCache(
      CacheNamespaceRegistry registry,
      CacheInvalidationBus bus,
      long maximumSize,
      Duration expireAfterWrite) {
    this.registry = registry;
    this.bus = bus;
    this.entries =
        Caffeine.newBuilder().maximumSize(maximumSize).expireAfterWrite(expireAfterWrite).build();
    bus.subscribe(this::receive);
  }

  private void broadcast(String type, String argument) {
    try {
      bus.publish(origin + SEPARATOR + type + SEPARATOR + argument);
    } catch (Exception e) {
      log.warn("Failed to broadcast memory cache invalidation {} {}", type, argument, e);
    }
  }

  private void receive(String message) {
    var parts = message == null ? null : message.split("\\" + SEPARATOR, 3);
    if (parts == null || parts.length != 3) {
      log.warn("Ignoring malformed memory cache invalidation: {}", message);
      return;
    }

    if (origin.equals(parts[0])) return;

    try {
      switch (parts[1]) {
        case KEY -> dropKey(parts[2]);
        case TENANT -> {
          var tenantId = Long.parseLong(parts[2]);
          registry.forgetTenant(tenantId);
          dropTenant(tenantId);
        }
        case ALL -> {
          registry.forgetAll();
          dropAll();
        }
        default -> log.warn("Ignoring unknown memory cache invalidation: {}", message);
      }
    } catch (NumberFormatException e) {
      log.warn("Ignoring malformed memory cache invalidation: {}", message);
    }
  }

  private Entry<V> store(String key, long tenantId, String namespace, V value) {
    if (namespace == null) return null;

    var entry = new Entry<>(tenantId, namespace, value);
    entries.put(key, entry);
    return entry;
  }

  private void dropKey(String key) {
    generation.incrementAndGet();
    entries.invalidate(key);
  }

  private void dropTenant(long tenantId) {
    generation.incrementAndGet();
    entries.asMap().values().removeIf(entry -> entry.tenantId() == tenantId);
  }

  private void dropAll() {
    generation.incrementAndGet();
    entries.invalidateAll();
  }

  /**
   * Returns a token identifying the current state of invalidations, to be passed to {@link
   * #promote} after reading the shared tier.
   *
   * @return the current invalidation generation.
   */
  public long generation() {
    return generation.get();
  }

  /**
   * Returns a cached value while the namespace it was stamped with is still current.
   *
   * @param key the cache key.
   * @return the value, or empty if absent, expired or detached by a namespace advance.
   */
  public Optional<V> get(String key) {
    var entry = entries.getIfPresent(key);
    if (entry == null) return Optional.empty();

    if (!registry.isCurrent(entry.tenantId(), entry.namespace())) {
      entries.asMap().remove(key, entry);
      return Optional.empty();
    }

    return Optional.of(entry.value());
  }

  /**
   * Stores a value freshly written to the shared tier.
   *
   * @param key the cache key.
   * @param tenantId the tenant owning the value.
   * @param namespace the namespace the value was stamped with in the shared tier.
   * @param value the value to cache.
   */
  public void put(String key, long tenantId, String namespace, V value) {
    store(key, tenantId, namespace, value);
  }

  /**
   * Stores a value read from the shared tier, unless an invalidation happened since {@code
   * generation} was taken.
   *
   * @param key the cache key.
   * @param tenantId the tenant owning the value.
   * @param namespace the namespace the value was stamped with in the shared tier.
   * @param value the value to cache.
   * @param generation the token taken with {@link #generation()} before the Redis read.
   */
  public void promote(String key, long tenantId, String namespace, V value, long generation) {
    if (this.generation.get() != generation) return;

    var entry = store(key, tenantId, namespace, value);
    if (entry != null && this.generation.get() != generation) entries.asMap().remove(key, entry);
  }

  /**
   * Drops an entry on this instance and on every other one.
   *
   * @param key the cache key.
   */
  public void invalidate(String key) {
    dropKey(key);
    broadcast(KEY, key);
  }

  /**
   * Advances the tenant's namespace, which detaches its entries in every tier on every instance,
   * drops them here and makes the other instances re-read the namespace at once.
   *
   * @param tenantId the tenant whose entries should be dropped.
   */
  public void invalidateTenant(long tenantId) {
    registry.invalidateTenant(tenantId);
    dropTenant(tenantId);
    broadcast(TENANT, String.valueOf(tenantId));
  }

  /**
   * Advances the global namespace, which detaches every entry in every tier on every instance,
   * drops them here and makes the other instances re-read all namespaces at once.
   */
  public void invalidateAll() {
    registry.invalidateAll();
    dropAll();
    broadcast(ALL, "");
  }
}
