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
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.asc.common.utilities.cache.bus.CacheInvalidationBus;
import java.time.Duration;
import java.util.Optional;
import java.util.concurrent.atomic.AtomicLong;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.dao.QueryTimeoutException;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.data.redis.serializer.SerializationException;

class CompositeCacheOutageTest {
  private static final long TENANT = 1L;
  private static final String ID = "client";
  private static final String KEY = "test:client:" + ID;
  private static final Duration WINDOW = Duration.ofSeconds(5);

  private static class StringCache extends CompositeCache<String> {
    StringCache(
        CacheInvalidationBus bus,
        CacheNamespaceRegistry registry,
        RedisTemplate<String, Object> redisTemplate) {
      super("test:client", bus, registry, redisTemplate);
    }

    @Override
    protected Long tenantOf(String value) {
      return TENANT;
    }

    @Override
    protected Object toStored(String namespace, String value) {
      return namespace + "|" + value;
    }

    @Override
    protected Stamped<String> fromStored(Object stored) {
      if (!(stored instanceof String s)) return null;
      var parts = s.split("\\|", 2);
      return new Stamped<>(parts[0], parts[1]);
    }

    void put(String value) {
      write(ID, value);
    }

    void evict() {
      remove(ID);
    }

    /** One lookup as the services make it: the cache first, then a ticket for the fill. */
    Optional<String> lookUp() {
      var cached = read(ID);
      if (cached.isEmpty()) ticket(ID);
      return cached;
    }
  }

  private final AtomicLong nanos = new AtomicLong();
  private InMemoryCacheSupport.Server server;
  private InMemoryCacheSupport.Redis redis;
  private CacheNamespaceRegistry registry;
  private StringCache cache;

  @BeforeEach
  void setUp() {
    server = new InMemoryCacheSupport.Server();
    redis = new InMemoryCacheSupport.Redis(server);
    registry =
        new CacheNamespaceRegistry(
            new InMemoryCacheSupport.CounterStore(server),
            "test:ver",
            new RedisOutageLatch(WINDOW, nanos::get));
    cache = new StringCache(new InMemoryCacheSupport.Bus(), registry, redis);
  }

  private void redisTimesOut() {
    server.failure = new QueryTimeoutException("Redis command timed out");
  }

  private int callsDuring(Runnable action) {
    var before = server.calls.get();
    action.run();
    return server.calls.get() - before;
  }

  @Test
  void givenRedisTimingOut_whenLookingUpWithColdNamespace_thenRedisTriedOnce() {
    cache.put("value");
    registry.forgetAll();
    redisTimesOut();

    assertEquals(1, callsDuring(cache::lookUp));
    assertEquals(0, callsDuring(cache::lookUp));
    assertNull(cache.ticket(ID).version());
  }

  @Test
  void givenRedisTimingOut_whenReadingEntry_thenItIsKeptAndTicketDoesNotTryAgain() {
    registry.namespaceOf(TENANT);
    redis.data.put(KEY, "ns|value");
    redisTimesOut();

    assertEquals(1, callsDuring(cache::lookUp));
    assertEquals("ns|value", redis.data.get(KEY));
  }

  @Test
  void givenEntryThatCannotBeDeserialized_whenReading_thenRemovedAndRedisStaysInUse() {
    registry.namespaceOf(TENANT);
    redis.data.put(KEY, "ns|value");
    redis.unreadable = new SerializationException("cannot deserialize");

    assertTrue(cache.lookUp().isEmpty());

    assertFalse(redis.data.containsKey(KEY));
    assertNotNull(cache.ticket(ID).version());
  }

  @Test
  void givenWindowHasPassed_whenRedisBack_thenOneProbeClosesLatch() {
    redisTimesOut();
    cache.lookUp();
    server.failure = null;

    assertEquals(0, callsDuring(cache::lookUp));

    nanos.addAndGet(WINDOW.toNanos());

    assertTrue(callsDuring(cache::lookUp) > 1);
    assertNotNull(cache.ticket(ID).version());
  }

  @Test
  void givenWindowHasPassed_whenRedisStillDown_thenProbeReopensIt() {
    redisTimesOut();
    cache.lookUp();
    nanos.addAndGet(WINDOW.toNanos());

    assertEquals(1, callsDuring(cache::lookUp));
    assertEquals(0, callsDuring(cache::lookUp));
  }

  @Test
  void givenLatchIsOpen_whenEvicting_thenEvictionStillReachesRedis() {
    redisTimesOut();
    cache.lookUp();
    server.failure = null;
    redis.data.put(KEY, "ns|value");

    cache.evict();

    assertFalse(redis.data.containsKey(KEY));
    assertEquals("1", redis.data.get(KEY + ":version"));
    assertNotNull(cache.ticket(ID).version());
  }
}
