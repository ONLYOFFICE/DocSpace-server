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

import java.time.Duration;
import java.util.List;

/**
 * Storage port for the monotonic counters a {@link CacheNamespaceRegistry} advances to invalidate
 * cache entries.
 *
 * <p>{@link CacheNamespaceRegistry} contains the whole namespace-versioning algorithm and has no
 * idea what backs a counter; implementations of this port supply that backend (Redis in practice).
 * Counters are plain strings so a fresh one can be minted as a timestamp without the store needing
 * to understand numeric increments.
 */
public interface CacheNamespaceCounterStore {

  /**
   * Reads several counters in a single round trip.
   *
   * @param keys the counter keys to read, in order.
   * @return the values in the same order as {@code keys}; a missing counter is {@code null} at its
   *     position. May itself be {@code null} if the backend reports nothing for the batch.
   */
  List<String> multiGet(List<String> keys);

  /**
   * Reads a single counter.
   *
   * @param key the counter key to read.
   * @return the counter's current value, or {@code null} if it does not exist.
   */
  String get(String key);

  /**
   * Creates a counter with the given value and TTL, but only if it does not already exist.
   *
   * @param key the counter key to create.
   * @param value the value to create it with.
   * @param ttl how long the new counter should live.
   * @return {@code true} if this call created the counter, {@code false} if another writer already
   *     had.
   */
  boolean setIfAbsent(String key, String value, Duration ttl);

  /**
   * Atomically advances a counter — creating it first if it does not exist — and (re)sets its TTL
   * to run from now, so a counter under continuing use never lapses mid-flight.
   *
   * @param key the counter key to advance.
   * @param ttl how long the counter should live after being advanced.
   */
  void increment(String key, Duration ttl);

  /**
   * Slides a counter's TTL forward without changing its value, so an idle-timeout only starts once
   * nothing reads the counter anymore.
   *
   * @param key the counter key to refresh.
   * @param ttl how long the counter should live from now.
   */
  void refreshTtl(String key, Duration ttl);
}
