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

package com.asc.common.utilities.cleanup;

import com.github.benmanes.caffeine.cache.Cache;
import com.github.benmanes.caffeine.cache.Caffeine;
import java.time.Clock;
import java.time.Duration;
import java.time.Instant;
import java.util.Optional;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.RejectedExecutionException;
import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;
import java.util.function.Supplier;
import lombok.extern.slf4j.Slf4j;

/**
 * Requests the cleanup of a removed client's leftovers once a lookup has shown the client is gone.
 *
 * <p>Callers report a client only when a lookup they made anyway answered that the client does not
 * exist, never when it was merely disabled or could not be reached, so reporting costs no extra
 * read of the client store. Each report comes with the time its leftovers were last written, either
 * known already or looked up on demand: leftovers younger than the grace period are left alone,
 * because right after a client is written an eventually consistent store can still answer that it
 * does not exist, and a client with no leftovers needs no cleanup at all.
 *
 * <p>A client is looked at most once per grace period, however often it is seen, and the leftovers
 * lookup and the cleanup request run off the caller's thread. Whatever the outcome - nothing left,
 * leftovers too young, cleanup requested - the next sighting after the grace period looks again, so
 * leftovers written later, as late writes of a removed client are, are still caught. Only a request
 * that failed is forgotten at once, so that the very next sighting retries it.
 *
 * <p>At most a fixed number of lookups run at a time. Each one takes a database connection from the
 * pool the requests use, so a flood of sightings - unknown client IDs sent faster than the memory
 * can absorb them - must not be able to take them all: a sighting that finds no free slot is
 * skipped and forgotten, and a later one reports the client again.
 */
@Slf4j
public class RemovedClientReporter implements AutoCloseable {
  private static final int MAX_REMEMBERED = 10_000;

  private final Consumer<String> cleanup;
  private final Duration grace;
  private final ExecutorService executor;
  private final Clock clock;
  private final Cache<String, Boolean> reported;
  private final Semaphore slots;

  public RemovedClientReporter(
      Consumer<String> cleanup, Duration grace, ExecutorService executor, int maxConcurrent) {
    this(cleanup, grace, executor, maxConcurrent, Clock.systemUTC());
  }

  RemovedClientReporter(
      Consumer<String> cleanup,
      Duration grace,
      ExecutorService executor,
      int maxConcurrent,
      Clock clock) {
    this.cleanup = cleanup;
    this.slots = new Semaphore(maxConcurrent);
    this.grace = grace;
    this.executor = executor;
    this.clock = clock;
    this.reported =
        Caffeine.newBuilder()
            .maximumSize(MAX_REMEMBERED)
            .expireAfterWrite(grace)
            .ticker(() -> TimeUnit.MILLISECONDS.toNanos(clock.millis()))
            .build();
  }

  private boolean isYoung(Instant written) {
    return written.isAfter(clock.instant().minus(grace));
  }

  private void requestCleanup(String clientId, Supplier<Optional<Instant>> lastLeftoverWrite) {
    try {
      var written = lastLeftoverWrite.get();
      if (written.isEmpty() || isYoung(written.get())) return;

      cleanup.accept(clientId);
      log.info("Requested the cleanup of removed client {}", clientId);
    } catch (Exception e) {
      reported.invalidate(clientId);
      log.warn("Failed to request the cleanup of removed client {}", clientId, e);
    }
  }

  private void submit(String clientId, Supplier<Optional<Instant>> lastLeftoverWrite) {
    if (clientId == null || clientId.isBlank()) return;
    if (reported.asMap().putIfAbsent(clientId, Boolean.TRUE) != null) return;

    if (!slots.tryAcquire()) {
      reported.invalidate(clientId);
      return;
    }

    try {
      executor.execute(
          () -> {
            try {
              requestCleanup(clientId, lastLeftoverWrite);
            } finally {
              slots.release();
            }
          });
    } catch (RejectedExecutionException e) {
      slots.release();
      reported.invalidate(clientId);
      log.warn("Could not schedule the cleanup of removed client {}", clientId, e);
    }
  }

  /**
   * Reports a client that a lookup has just shown to be gone.
   *
   * @param clientId the ID of the removed client.
   * @param lastModified when the leftover row that led to the lookup was last written, or {@code
   *     null} if unknown.
   */
  public void report(String clientId, Instant lastModified) {
    if (lastModified != null && isYoung(lastModified)) return;

    submit(clientId, () -> Optional.of(lastModified == null ? Instant.EPOCH : lastModified));
  }

  /**
   * Reports a client that a lookup has just shown to be gone, without knowing yet whether it left
   * anything behind.
   *
   * @param clientId the ID of the removed client.
   * @param lastLeftoverWrite looks up when the client's leftovers were last written; empty if it
   *     left nothing behind. It runs off the caller's thread.
   */
  public void reportIfLeftovers(String clientId, Supplier<Optional<Instant>> lastLeftoverWrite) {
    submit(clientId, lastLeftoverWrite);
  }

  @Override
  public void close() {
    executor.shutdown();
  }
}
