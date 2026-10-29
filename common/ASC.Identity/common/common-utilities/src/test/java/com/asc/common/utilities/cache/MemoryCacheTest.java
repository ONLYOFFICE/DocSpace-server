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

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.asc.common.utilities.cache.bus.CacheInvalidationBus;
import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Consumer;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class MemoryCacheTest {
  private static final long TENANT = 1L;
  private static final long OTHER_TENANT = 2L;
  private static final Duration TTL = Duration.ofMinutes(1);

  private static class InMemoryCounterStore implements CacheNamespaceCounterStore {
    private final Map<String, String> counters = new ConcurrentHashMap<>();

    @Override
    public List<String> multiGet(List<String> keys) {
      return keys.stream().map(counters::get).toList();
    }

    @Override
    public String get(String key) {
      return counters.get(key);
    }

    @Override
    public boolean setIfAbsent(String key, String value, Duration ttl) {
      return counters.putIfAbsent(key, value) == null;
    }

    @Override
    public void increment(String key, Duration ttl) {
      counters.merge(key, "1", (a, b) -> String.valueOf(Long.parseLong(a) + 1));
    }

    @Override
    public void refreshTtl(String key, Duration ttl) {}
  }

  private static class InMemoryBus implements CacheInvalidationBus {
    private final List<Consumer<String>> listeners = new ArrayList<>();

    @Override
    public void publish(String message) {
      listeners.forEach(listener -> listener.accept(message));
    }

    @Override
    public void subscribe(Consumer<String> listener) {
      listeners.add(listener);
    }
  }

  private CacheNamespaceRegistry registryA;
  private CacheNamespaceRegistry registryB;
  private MemoryCache<String> cacheA;
  private MemoryCache<String> cacheB;

  @BeforeEach
  void setUp() {
    var store = new InMemoryCounterStore();
    var bus = new InMemoryBus();
    registryA = new CacheNamespaceRegistry(store, "test:ver");
    registryB = new CacheNamespaceRegistry(store, "test:ver");
    cacheA = new MemoryCache<>(registryA, bus, 100, TTL);
    cacheB = new MemoryCache<>(registryB, bus, 100, TTL);
  }

  private void put(MemoryCache<String> cache, CacheNamespaceRegistry registry, String key, long t) {
    cache.put(key, t, registry.namespaceOf(t), key + "-value");
  }

  @Test
  void givenStoredValue_whenNamespaceIsCurrent_thenReturnsValue() {
    put(cacheA, registryA, "a", TENANT);

    assertEquals(Optional.of("a-value"), cacheA.get("a"));
  }

  @Test
  void givenKeyCachedOnBothInstances_whenInvalidatingKey_thenDropsItOnEveryInstance() {
    put(cacheA, registryA, "a", TENANT);
    put(cacheB, registryB, "a", TENANT);

    cacheA.invalidate("a");

    assertTrue(cacheA.get("a").isEmpty());
    assertTrue(cacheB.get("a").isEmpty());
  }

  @Test
  void givenEntriesOfTwoTenants_whenInvalidatingTenant_thenDropsOnlyThatTenantOnEveryInstance() {
    put(cacheB, registryB, "a", TENANT);
    put(cacheB, registryB, "b", OTHER_TENANT);

    cacheA.invalidateTenant(TENANT);

    assertTrue(cacheB.get("a").isEmpty());
    assertEquals(Optional.of("b-value"), cacheB.get("b"));
  }

  @Test
  void givenCachedEntry_whenNamespaceAdvancesWithoutBroadcast_thenEntryIsDetached() {
    put(cacheB, registryB, "a", TENANT);

    registryA.invalidateTenant(TENANT);
    registryB.forgetTenant(TENANT);

    assertTrue(cacheB.get("a").isEmpty());
  }

  @Test
  void givenEntriesOfTwoTenants_whenInvalidatingAll_thenDropsEveryEntryOnEveryInstance() {
    put(cacheB, registryB, "a", TENANT);
    put(cacheB, registryB, "b", OTHER_TENANT);

    cacheA.invalidateAll();

    assertTrue(cacheB.get("a").isEmpty());
    assertTrue(cacheB.get("b").isEmpty());
  }

  @Test
  void givenInvalidationDuringRedisRead_whenPromoting_thenEntryIsNotKept() {
    var generation = cacheB.generation();
    cacheA.invalidate("a");

    cacheB.promote("a", TENANT, registryB.namespaceOf(TENANT), "stale", generation);

    assertTrue(cacheB.get("a").isEmpty());
  }

  @Test
  void givenNoInvalidation_whenPromoting_thenEntryIsKept() {
    var generation = cacheB.generation();

    cacheB.promote("a", TENANT, registryB.namespaceOf(TENANT), "fresh", generation);

    assertEquals(Optional.of("fresh"), cacheB.get("a"));
  }

  @Test
  void givenNoNamespace_whenPutting_thenEntryIsNotStored() {
    cacheA.put("a", TENANT, null, "value");

    assertTrue(cacheA.get("a").isEmpty());
  }
}
