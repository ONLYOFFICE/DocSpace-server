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
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.asc.common.utilities.cache.bus.CacheInvalidationBus;
import java.util.Optional;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.data.redis.core.RedisTemplate;

class CompositeCacheTest {
  private static final long TENANT = 1L;
  private static final String ID = "client";
  private static final String KEY = "test:client:" + ID;

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

    Optional<String> readValue() {
      return read(ID);
    }

    void cache(String value, CacheTicket ticket) {
      writeUnlessInvalidated(ID, value, ticket);
    }

    void put(String value) {
      write(ID, value);
    }

    void evict() {
      remove(ID);
    }
  }

  private InMemoryCacheSupport.Redis redis;
  private StringCache cacheA;
  private StringCache cacheB;

  @BeforeEach
  void setUp() {
    var store = new InMemoryCacheSupport.CounterStore();
    var bus = new InMemoryCacheSupport.Bus();
    redis = new InMemoryCacheSupport.Redis();
    cacheA = new StringCache(bus, new CacheNamespaceRegistry(store, "test:ver"), redis);
    cacheB = new StringCache(bus, new CacheNamespaceRegistry(store, "test:ver"), redis);
  }

  @Test
  void givenNoInvalidation_whenCachingAReadValue_thenBothTiersHoldIt() {
    var ticket = cacheA.ticket(ID);

    cacheA.cache("fresh", ticket);

    assertTrue(redis.data.get(KEY).endsWith("|fresh"));

    redis.data.clear();

    assertEquals(Optional.of("fresh"), cacheA.readValue());
  }

  @Test
  void givenEvictionBeforeTheWrite_whenCachingAReadValue_thenNothingIsWritten() {
    var ticket = cacheA.ticket(ID);
    cacheB.evict();

    cacheA.cache("stale", ticket);

    assertTrue(redis.writes.isEmpty());
  }

  @Test
  void givenEvictionWhoseBroadcastIsLost_whenCachingAReadValue_thenRedisRefusesIt() {
    var ticket = cacheA.ticket(ID);
    redis.data.put(KEY + ":version", String.valueOf(ticket.version() + 1));

    cacheA.cache("stale", ticket);

    assertFalse(redis.data.containsKey(KEY));
    assertTrue(cacheA.readValue().isEmpty());
  }

  @Test
  void givenEvictionRightBeforeTheRedisWrite_whenCachingAReadValue_thenRedisRefusesIt() {
    var ticket = cacheA.ticket(ID);
    redis.beforeScript = cacheB::evict;

    cacheA.cache("stale", ticket);

    assertTrue(redis.writes.isEmpty());
    assertTrue(cacheA.readValue().isEmpty());
  }

  @Test
  void givenWriteOfACurrentValueDuringTheRead_whenCachingTheReadValue_thenTheCurrentValueStays() {
    var ticket = cacheA.ticket(ID);
    cacheB.put("current");

    cacheA.cache("stale", ticket);

    assertTrue(redis.data.get(KEY).endsWith("|current"));
  }

  @Test
  void givenRedisUnreadableWhenTakingTheTicket_whenCachingAReadValue_thenNothingIsWritten() {
    cacheA.cache("value", new CacheTicket(cacheA.ticket(ID).generation(), null));

    assertTrue(redis.writes.isEmpty());
  }
}
