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
import java.util.concurrent.atomic.AtomicLong;
import java.util.function.LongSupplier;

/**
 * Remembers for a short window that Redis could not be reached, so that cache lookups stop paying a
 * full command timeout on every call while it is down.
 *
 * <p>A lookup makes several Redis calls in a row (the namespace counters, the entry, the version
 * ticket). Without the latch each of them waits out its own timeout before the lookup falls back to
 * the source. Once one call fails, the latch opens and the read-only calls that follow skip Redis
 * at once, treating it as a miss. When the window has passed, exactly one call is let through as a
 * probe: if it succeeds the latch closes, if it fails the latch stays open for another window.
 *
 * <p>Only lookups consult the latch. Evictions always go to Redis, as skipping one that would have
 * succeeded could leave a stale entry behind; their outcome still opens or closes the latch.
 */
final class RedisOutageLatch {
  static final Duration DEFAULT_WINDOW = Duration.ofSeconds(5);
  private static final long CLOSED = Long.MIN_VALUE;

  private final long windowNanos;
  private final LongSupplier nanoClock;
  private final AtomicLong reopensAt = new AtomicLong(CLOSED);

  RedisOutageLatch() {
    this(DEFAULT_WINDOW, System::nanoTime);
  }

  /**
   * @param window how long lookups skip Redis after a failure.
   * @param nanoClock a monotonic clock in nanoseconds.
   */
  RedisOutageLatch(Duration window, LongSupplier nanoClock) {
    this.windowNanos = window.toNanos();
    this.nanoClock = nanoClock;
  }

  /**
   * Tells whether a lookup may call Redis now. While the latch is open this is {@code false},
   * except for the single caller that gets to probe Redis once the window has passed.
   *
   * @return {@code true} if the caller should call Redis.
   */
  boolean permits() {
    var until = reopensAt.get();
    if (until == CLOSED) return true;

    var now = nanoClock.getAsLong();
    if (now - until < 0) return false;

    return reopensAt.compareAndSet(until, now + windowNanos);
  }

  /** Records that a Redis call failed to reach Redis, opening the latch for one window. */
  void trip() {
    reopensAt.set(nanoClock.getAsLong() + windowNanos);
  }

  /** Records that a Redis call succeeded, closing the latch. */
  void reset() {
    if (reopensAt.get() != CLOSED) reopensAt.set(CLOSED);
  }
}
