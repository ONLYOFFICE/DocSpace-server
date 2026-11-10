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

package com.asc.registration.messaging.listener;

import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.doAnswer;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientCacheRemoveEvent;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientUpdatedEvent;
import com.asc.registration.core.domain.value.ClientTenantInfo;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import java.time.Duration;
import java.time.ZonedDateTime;
import java.util.UUID;
import java.util.concurrent.CountDownLatch;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ClientCacheEventListenerTest {
  private AuthorizationMessagePublisher<ClientCacheRemoveEvent> publisher;
  private ClientCacheEventListener listener;
  private ClientUpdatedEvent event;
  private final CountDownLatch release = new CountDownLatch(1);

  @BeforeEach
  @SuppressWarnings("unchecked")
  void setUp() {
    publisher = mock(AuthorizationMessagePublisher.class);
    listener = new ClientCacheEventListener(mock(ClientCacheService.class), publisher);

    var client = mock(Client.class);
    when(client.getId()).thenReturn(new ClientId(UUID.randomUUID()));
    when(client.getClientTenantInfo()).thenReturn(new ClientTenantInfo(new TenantId(1L)));
    event = new ClientUpdatedEvent(null, client, ZonedDateTime.now());
  }

  @AfterEach
  void tearDown() {
    release.countDown();
    listener.close();
  }

  private Duration timeEvictBeforeCommit() {
    var started = System.nanoTime();
    listener.evictBeforeUpdateCommits(event);
    return Duration.ofNanos(System.nanoTime() - started);
  }

  @Test
  void givenSlowBroadcast_whenUpdateCommits_thenWaitsForIt() {
    doAnswer(
            invocation -> {
              Thread.sleep(100);
              return null;
            })
        .when(publisher)
        .publishNow(any());

    var took = timeEvictBeforeCommit();

    assertTrue(took.toMillis() >= 100, "the commit did not wait for the broadcast: " + took);
  }

  @Test
  void givenStuckBroadcast_whenUpdateCommits_thenWaitsNoLongerThanTheLimit() {
    doAnswer(
            invocation -> {
              release.await();
              return null;
            })
        .when(publisher)
        .publishNow(any());

    var took = timeEvictBeforeCommit();

    assertTrue(took.toMillis() >= 200, "the commit did not wait: " + took);
    assertTrue(took.toMillis() < 1_000, "the commit waited for a stuck broadcast: " + took);
  }
}
