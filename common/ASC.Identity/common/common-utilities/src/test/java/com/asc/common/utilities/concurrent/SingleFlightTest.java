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

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assertions.fail;

import java.time.Duration;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;
import org.junit.jupiter.api.Test;

public class SingleFlightTest {
  private static final String KEY = "key";
  private static final Duration WAIT = Duration.ofSeconds(10);

  private static void awaitAllParked(List<Thread> threads) {
    var deadline = System.nanoTime() + TimeUnit.SECONDS.toNanos(10);
    while (System.nanoTime() < deadline) {
      var parked =
          threads.stream()
              .filter(
                  thread ->
                      thread.getState() == Thread.State.WAITING
                          || thread.getState() == Thread.State.TIMED_WAITING)
              .count();
      if (parked == threads.size()) return;
      Thread.onSpinWait();
    }

    fail("Threads did not park while a call was in progress");
  }

  private static List<Thread> startAll(int count, Runnable body) {
    var threads = new ArrayList<Thread>();
    for (var i = 0; i < count; i++) {
      var thread = new Thread(body);
      threads.add(thread);
      thread.start();
    }

    return threads;
  }

  private static void joinAll(List<Thread> threads) throws InterruptedException {
    for (var thread : threads) thread.join(TimeUnit.SECONDS.toMillis(10));
  }

  private static String awaitPeer(CountDownLatch bothEntered) {
    try {
      bothEntered.countDown();
      assertTrue(bothEntered.await(10, TimeUnit.SECONDS), "Keys did not run concurrently");
      return "value";
    } catch (InterruptedException e) {
      Thread.currentThread().interrupt();
      throw new IllegalStateException(e);
    }
  }

  @Test
  void whenCallersOverlapOnOneKey_thenLoaderRunsOnce() throws Exception {
    var singleFlight = new SingleFlight<String, String>(WAIT);
    var runs = new AtomicInteger();
    var entered = new CountDownLatch(1);
    var release = new CountDownLatch(1);
    var results = Collections.synchronizedList(new ArrayList<String>());

    var callers = 16;
    var threads =
        startAll(
            callers,
            () ->
                results.add(
                    singleFlight.execute(
                        KEY,
                        () -> {
                          runs.incrementAndGet();
                          entered.countDown();
                          try {
                            assertTrue(release.await(10, TimeUnit.SECONDS), "Never released");
                          } catch (InterruptedException e) {
                            Thread.currentThread().interrupt();
                          }
                          return "value";
                        })));

    assertTrue(entered.await(10, TimeUnit.SECONDS), "The loader was never entered");

    awaitAllParked(threads);
    release.countDown();
    joinAll(threads);

    assertEquals(1, runs.get(), "Overlapping callers should share a single run");
    assertEquals(callers, results.size(), "Every caller should receive the value");
    assertTrue(results.stream().allMatch("value"::equals));
  }

  @Test
  void whenCallersOverlapOnDifferentKeys_thenNeitherWaitsOnOther() throws Exception {
    var singleFlight = new SingleFlight<String, String>(WAIT);
    var bothEntered = new CountDownLatch(2);

    var threads =
        List.of(
            new Thread(() -> singleFlight.execute("first", () -> awaitPeer(bothEntered))),
            new Thread(() -> singleFlight.execute("second", () -> awaitPeer(bothEntered))));
    threads.forEach(Thread::start);

    joinAll(threads);

    assertEquals(0, bothEntered.getCount(), "Both keys should have run at the same time");
  }

  @Test
  void whenCallsDoNotOverlap_thenLoaderRunsEachTime() {
    var singleFlight = new SingleFlight<String, String>(WAIT);
    var runs = new AtomicInteger();

    for (var i = 0; i < 3; i++)
      assertEquals(
          "value",
          singleFlight.execute(
              KEY,
              () -> {
                runs.incrementAndGet();
                return "value";
              }));

    assertEquals(3, runs.get(), "Nothing is retained between calls that do not overlap");
  }

  @Test
  void whenLoaderFails_thenWaitersSeeSameFailure() throws Exception {
    var singleFlight = new SingleFlight<String, String>(WAIT);
    var failure = new IllegalArgumentException("loader failed");
    var entered = new CountDownLatch(1);
    var release = new CountDownLatch(1);
    var observed = Collections.synchronizedList(new ArrayList<Throwable>());

    var callers = 8;
    var threads =
        startAll(
            callers,
            () -> {
              try {
                singleFlight.execute(
                    KEY,
                    () -> {
                      entered.countDown();
                      try {
                        assertTrue(release.await(10, TimeUnit.SECONDS), "Never released");
                      } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                      }
                      throw failure;
                    });
                fail("The failure should have propagated");
              } catch (Throwable e) {
                observed.add(e);
              }
            });

    assertTrue(entered.await(10, TimeUnit.SECONDS), "The loader was never entered");

    awaitAllParked(threads);
    release.countDown();
    joinAll(threads);

    assertEquals(callers, observed.size(), "Every caller should see a failure");
    assertTrue(
        observed.stream().allMatch(e -> e == failure),
        "Waiters should see the exception the loader threw, not a wrapper");
  }

  @Test
  void whenWaitElapses_thenWaiterRunsTheLoaderItself() throws Exception {
    var singleFlight = new SingleFlight<String, String>(Duration.ofMillis(50));
    var entered = new CountDownLatch(1);
    var release = new CountDownLatch(1);

    var runner =
        new Thread(
            () ->
                singleFlight.execute(
                    KEY,
                    () -> {
                      entered.countDown();
                      try {
                        assertTrue(release.await(10, TimeUnit.SECONDS), "Never released");
                      } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                      }
                      return "value";
                    }));

    runner.start();

    try {
      assertTrue(entered.await(10, TimeUnit.SECONDS), "The loader was never entered");

      var resolved = new AtomicReference<String>();
      var waiter = new Thread(() -> resolved.set(singleFlight.execute(KEY, () -> "fallback")));
      waiter.start();
      waiter.join(TimeUnit.SECONDS.toMillis(10));

      assertEquals("fallback", resolved.get(), "A waiter that gives up should run the loader");
    } finally {
      release.countDown();
      runner.join(TimeUnit.SECONDS.toMillis(10));
    }
  }
}
