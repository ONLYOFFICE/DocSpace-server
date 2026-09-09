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

package com.asc.common.utilities.concurrent;

import java.time.Duration;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.function.Supplier;

/**
 * Collapses concurrent calls for the same key into a single execution, so that a burst of callers
 * that all miss a cache costs the service behind it one call instead of one call each.
 *
 * <p>The first caller to arrive for a key runs the loader itself; callers that arrive for the same
 * key while it is still running wait for that result rather than starting their own. Callers for
 * different keys never wait on one another, and the loader runs on the calling thread, so it stays
 * inside whatever context the caller established.
 *
 * @param <K> the key that identifies a call.
 * @param <V> the value the loader produces.
 */
public final class SingleFlight<K, V> {
  private final long waitMs;
  private final ConcurrentMap<K, CompletableFuture<V>> inFlight = new ConcurrentHashMap<>();

  public SingleFlight(Duration wait) {
    this.waitMs = wait.toMillis();
  }

  private V join(K key, CompletableFuture<V> inProgress, Supplier<V> loader) {
    try {
      return inProgress.get(waitMs, TimeUnit.MILLISECONDS);
    } catch (ExecutionException e) {
      var cause = e.getCause();
      if (cause instanceof RuntimeException runtime) throw runtime;
      if (cause instanceof Error error) throw error;

      throw new SingleFlightException("Call in progress for key %s failed".formatted(key), cause);
    } catch (TimeoutException e) {
      // Allow the call on a timeout. Treat it as a miss
      return loader.get();
    } catch (InterruptedException e) {
      Thread.currentThread().interrupt();
      throw new SingleFlightException(
          "Interrupted while waiting for a call in progress for key %s".formatted(key), e);
    }
  }

  /**
   * Returns the value for a key, running the loader only if no call for that key is already in
   * progress.
   *
   * @param key the key identifying the call.
   * @param loader produces the value; run on the calling thread, by the first caller and by any
   *     caller whose wait elapses.
   * @return the value the loader produced, whether this caller ran it or waited for it.
   * @throws SingleFlightException if the wait for a call in progress is interrupted.
   */
  public V execute(K key, Supplier<V> loader) {
    var flight = new CompletableFuture<V>();
    var inProgress = inFlight.putIfAbsent(key, flight);
    if (inProgress != null) return join(key, inProgress, loader);

    try {
      var value = loader.get();
      flight.complete(value);
      return value;
    } catch (RuntimeException | Error e) {
      flight.completeExceptionally(e);
      throw e;
    } finally {
      inFlight.remove(key, flight);
    }
  }
}
