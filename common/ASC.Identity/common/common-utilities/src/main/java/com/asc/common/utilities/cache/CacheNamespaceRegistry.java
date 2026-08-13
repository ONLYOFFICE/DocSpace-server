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

import com.github.benmanes.caffeine.cache.Cache;
import com.github.benmanes.caffeine.cache.Caffeine;
import java.time.Duration;
import java.time.Instant;
import java.util.List;
import lombok.extern.slf4j.Slf4j;

/**
 * Resolves the cache namespace a tenant's entries belong to, for any Redis-backed cache that wants
 * scan-free eviction.
 *
 * <p>A namespace is the pair of two monotonic counters: a global one and one per tenant. Callers
 * stamp each cache entry with the namespace it was written under (in the key or in the value) and
 * accept an entry only while its stamp still matches the current namespace. Discarding entries is
 * then a matter of moving a counter forward rather than locating the keys to delete: everything
 * stamped with the previous namespace stops matching at once and is reclaimed by its own TTL,
 * wherever it lives. Bumping the tenant counter drops one tenant, bumping the global counter drops
 * all of them, and both cost a single counter advance regardless of how many entries are cached.
 *
 * <p>Namespaces are resolved on every cache operation, so they are memoized in-process for a short
 * window to keep the counter store off the hot path. The window bounds how long a caller may keep
 * addressing a superseded namespace; keep it far shorter than the lifetime of the cached entries.
 *
 * <p>A counter's TTL is a sliding idle-timeout, not a fixed lifetime: it is set on mint, refreshed
 * on every successful read, and refreshed again on every advance. A counter only lapses after a
 * full TTL with no activity at all. Without the read-time refresh, a counter backing entries under
 * continuous traffic would still expire on schedule and re-mint with a fresh value, instantly
 * detaching every entry live under it — a self-inflicted, periodic full-cache invalidation with no
 * eviction call behind it.
 *
 * <p>When the counter store cannot be reached, {@code null} is returned instead of a placeholder
 * value. A placeholder that every failed call shared would let two independent failures — one while
 * writing an entry, another while later reading it back — agree with each other and pass as a
 * match. {@code null} cannot be stamped onto a cached entry (callers should skip the write instead)
 * and can never equal a namespace read back from a cached entry, so a resolution failure always
 * fails closed.
 */
@Slf4j
public class CacheNamespaceRegistry {
  private static final String NAMESPACE_SEPARATOR = ".";
  private static final int LOCAL_MAX_TENANTS = 50_000;
  private static final Duration LOCAL_TTL = Duration.ofSeconds(1);
  private static final Duration VERSION_TTL = Duration.ofHours(1);

  private final String globalVersionKey;
  private final CacheNamespaceCounterStore store;
  private final Cache<Long, String> localNamespaces;

  /**
   * @param store the counter store backing this registry's counters.
   * @param keyPrefix the prefix identifying this registry's counters. The global counter lives at
   *     this key, tenant counters at {@code keyPrefix + ":" + tenantId}. Give every independent
   *     cache its own prefix so their counters cannot collide with one another.
   */
  public CacheNamespaceRegistry(CacheNamespaceCounterStore store, String keyPrefix) {
    this.store = store;
    this.globalVersionKey = keyPrefix;
    this.localNamespaces =
        Caffeine.newBuilder().maximumSize(LOCAL_MAX_TENANTS).expireAfterWrite(LOCAL_TTL).build();
  }

  private String tenantVersionKey(long tenantId) {
    return globalVersionKey + ":" + tenantId;
  }

  private String storedVersion(List<String> versions, int index) {
    if (versions == null || versions.size() <= index) return null;

    var version = versions.get(index);
    return version == null || version.isEmpty() ? null : version;
  }

  private void refreshTtl(String versionKey) {
    try {
      store.refreshTtl(versionKey, VERSION_TTL);
    } catch (Exception e) {
      log.warn("Failed to refresh TTL for cache namespace counter: {}", versionKey, e);
    }
  }

  private String resolveVersion(String versionKey, String storedVersion) {
    if (storedVersion != null) {
      refreshTtl(versionKey);
      return storedVersion;
    }

    var minted = String.valueOf(Instant.now().toEpochMilli());
    if (store.setIfAbsent(versionKey, minted, VERSION_TTL)) return minted;

    var concurrent = store.get(versionKey);
    return concurrent == null || concurrent.isEmpty() ? minted : concurrent;
  }

  private boolean advance(String versionKey) {
    try {
      store.increment(versionKey, VERSION_TTL);
      return true;
    } catch (Exception e) {
      log.error("Failed to advance cache namespace at key: {}", versionKey, e);
      return false;
    }
  }

  /**
   * Returns the namespace token identifying where a tenant's entries currently live.
   *
   * <p>Both counters are fetched in a single round trip, and an absent counter is minted rather
   * than assumed, so losing one can only detach entries instead of exposing entries it was meant to
   * invalidate.
   *
   * @param tenantId the tenant whose namespace to resolve.
   * @return the tenant's current namespace token, or {@code null} if it could not be resolved.
   */
  public String namespaceOf(long tenantId) {
    var memoized = localNamespaces.getIfPresent(tenantId);
    if (memoized != null) return memoized;

    var tenantVersionKey = tenantVersionKey(tenantId);
    try {
      var versions = store.multiGet(List.of(globalVersionKey, tenantVersionKey));
      var namespace =
          resolveVersion(globalVersionKey, storedVersion(versions, 0))
              + NAMESPACE_SEPARATOR
              + resolveVersion(tenantVersionKey, storedVersion(versions, 1));

      localNamespaces.put(tenantId, namespace);
      return namespace;
    } catch (Exception e) {
      log.warn("Failed to resolve cache namespace for tenant: {}", tenantId, e);
      return null;
    }
  }

  /**
   * Moves a single tenant to a new namespace, invalidating every entry cached under the previous
   * one.
   *
   * @param tenantId the tenant whose cached entries should be invalidated.
   */
  public void invalidateTenant(long tenantId) {
    if (advance(tenantVersionKey(tenantId)))
      log.info("Advanced cache namespace of tenant {} under prefix {}", tenantId, globalVersionKey);

    localNamespaces.invalidate(tenantId);
  }

  /** Moves every tenant to a new namespace, invalidating the cache as a whole. */
  public void invalidateAll() {
    if (advance(globalVersionKey))
      log.info("Advanced global cache namespace for prefix {}", globalVersionKey);

    localNamespaces.invalidateAll();
  }
}
