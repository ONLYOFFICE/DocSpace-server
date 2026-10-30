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

import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import com.asc.common.utilities.cache.bus.CacheInvalidationBus;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Consumer;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.data.redis.core.ValueOperations;
import org.springframework.data.redis.core.script.RedisScript;
import org.springframework.data.redis.serializer.RedisSerializer;

final class InMemoryCacheSupport {
  private InMemoryCacheSupport() {}

  static class CounterStore implements CacheNamespaceCounterStore {
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

  static class Bus implements CacheInvalidationBus {
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

  static class Redis extends RedisTemplate<String, Object> {
    final Map<String, String> data = new ConcurrentHashMap<>();
    final List<String> writes = new ArrayList<>();
    Runnable beforeScript = () -> {};

    Redis() {
      setValueSerializer(RedisSerializer.string());
    }

    @Override
    public ValueOperations<String, Object> opsForValue() {
      @SuppressWarnings("unchecked")
      ValueOperations<String, Object> values = mock(ValueOperations.class);
      when(values.get(org.mockito.ArgumentMatchers.anyString()))
          .thenAnswer(invocation -> data.get(invocation.<String>getArgument(0)));
      return values;
    }

    @Override
    public Boolean delete(String key) {
      return data.remove(key) != null;
    }

    @Override
    @SuppressWarnings("unchecked")
    public <T> T execute(
        RedisScript<T> script,
        RedisSerializer<?> argsSerializer,
        RedisSerializer<T> resultSerializer,
        List<String> keys,
        Object... args) {
      var hook = beforeScript;
      beforeScript = () -> {};
      hook.run();

      var value = keys.get(0);
      var version = keys.get(1);
      var current = data.getOrDefault(version, "0");
      Long result;
      if (script == CompositeCache.READ_VERSION) {
        result = Long.parseLong(current);
      } else if (script == CompositeCache.EVICT) {
        data.put(version, String.valueOf(Long.parseLong(current) + 1));
        data.remove(value);
        result = Long.parseLong(data.get(version));
      } else if (script == CompositeCache.WRITE) {
        data.put(version, String.valueOf(Long.parseLong(current) + 1));
        data.put(value, text(args[0]));
        writes.add(text(args[0]));
        result = Long.parseLong(data.get(version));
      } else if (script == CompositeCache.WRITE_IF_VERSION) {
        if (!current.equals(text(args[2]))) {
          result = 0L;
        } else {
          data.put(value, text(args[0]));
          writes.add(text(args[0]));
          result = 1L;
        }
      } else {
        throw new IllegalArgumentException("unknown script");
      }

      return (T) result;
    }

    private static String text(Object arg) {
      return new String((byte[]) arg, StandardCharsets.UTF_8);
    }
  }
}
