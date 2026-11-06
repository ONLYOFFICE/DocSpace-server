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

import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientCacheRemoveEvent;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientCreatedEvent;
import com.asc.registration.core.domain.event.ClientDeletedEvent;
import com.asc.registration.core.domain.event.ClientUpdatedEvent;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import java.time.Duration;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.event.TransactionPhase;
import org.springframework.transaction.event.TransactionalEventListener;

/**
 * ClientCacheEventListener listens for client domain events and updates the cache transactionally.
 *
 * <p>This listener handles cache population and cleanup based on client domain events. It processes
 * events after the transaction commits to ensure cache consistency with the database state. It also
 * publishes cache removal events to RabbitMQ for multi-region cache invalidation.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientCacheEventListener implements AutoCloseable {
  private static final Duration BEFORE_COMMIT_WAIT = Duration.ofMillis(200);

  private final ClientCacheService clientCacheService;
  private final AuthorizationMessagePublisher<ClientCacheRemoveEvent>
      clientCacheRemoveMessagePublisher;
  private final ExecutorService beforeCommitEvictions = Executors.newVirtualThreadPerTaskExecutor();

  /**
   * Handles client created events by populating the cache with the new client.
   *
   * @param event The client created event.
   */
  @TransactionalEventListener
  public void handleClientCreated(ClientCreatedEvent event) {
    log.debug(
        "Handling client created event for client ID: {}", event.getClient().getId().getValue());
    clientCacheService.put(event.getClient());
  }

  /**
   * Evicts a client here and, through the broadcast, in every other region, before its change
   * commits. Evicting an entry that turns out to be still valid only costs a refill, while a
   * process that dies right after the commit would otherwise leave every region serving the old
   * value until it expires. The eviction and broadcast after the commit still run.
   *
   * <p>The commit waits up to {@link #BEFORE_COMMIT_WAIT} for the local eviction and for the
   * broadcast to be handed to the broker, sent on this path rather than in the background so the
   * wait covers it. Past that, or if either fails, the change commits without them.
   *
   * <p>What this guarantees is that a cached client is stale no longer than its entry lives, and
   * usually not at all. A reader that loads the old row between this eviction and the commit can
   * still cache it; the eviction after the commit removes it, unless the process dies first, in
   * which case it stays until it expires.
   */
  private void evictBeforeCommit(Client client) {
    var tenantId =
        client.getClientTenantInfo() != null ? client.getClientTenantInfo().tenantId() : null;
    if (tenantId == null) return;

    var eviction =
        CompletableFuture.runAsync(
            () -> {
              clientCacheService.evict(client.getId(), tenantId);
              clientCacheRemoveMessagePublisher.publishNow(
                  ClientCacheRemoveEvent.builder()
                      .clientId(client.getId().getValue().toString())
                      .tenantId(tenantId.getValue())
                      .build());
            },
            beforeCommitEvictions);
    try {
      eviction.get(BEFORE_COMMIT_WAIT.toMillis(), TimeUnit.MILLISECONDS);
    } catch (TimeoutException e) {
      log.warn(
          "Eviction of client {} before the commit is taking longer than {}; committing without it",
          client.getId().getValue(),
          BEFORE_COMMIT_WAIT);
    } catch (InterruptedException e) {
      Thread.currentThread().interrupt();
    } catch (Exception e) {
      log.warn("Could not evict client {} before the commit", client.getId().getValue(), e);
    }
  }

  @Override
  public void close() {
    beforeCommitEvictions.close();
  }

  /**
   * Evicts an updated client in every region before the change commits, so that a process that dies
   * between the commit and {@link #handleClientUpdated} leaves no stale entry behind. A value
   * refilled from the old row in between is removed by the eviction after the commit.
   *
   * @param event The client updated event.
   */
  @TransactionalEventListener(phase = TransactionPhase.BEFORE_COMMIT)
  public void evictBeforeUpdateCommits(ClientUpdatedEvent event) {
    evictBeforeCommit(event.getClient());
  }

  /**
   * Evicts a deleted client in every region before the delete commits, so that a process that dies
   * between the commit and {@link #handleClientDeleted} does not leave the deleted client in any
   * cache until it expires. A value refilled from the old row in between is removed by the eviction
   * after the commit.
   *
   * @param event The client deleted event.
   */
  @TransactionalEventListener(phase = TransactionPhase.BEFORE_COMMIT)
  public void evictBeforeDeleteCommits(ClientDeletedEvent event) {
    evictBeforeCommit(event.getClient());
  }

  /**
   * Handles client updated events by evicting the cache and publishing a removal event for
   * multi-region cache invalidation.
   *
   * @param event The client updated event.
   */
  @TransactionalEventListener
  public void handleClientUpdated(ClientUpdatedEvent event) {
    log.debug(
        "Handling client updated event for client ID: {}", event.getClient().getId().getValue());
    var client = event.getClient();
    var tenantId =
        client.getClientTenantInfo() != null ? client.getClientTenantInfo().tenantId() : null;

    if (tenantId != null) {
      clientCacheService.evict(client.getId(), tenantId);
      clientCacheRemoveMessagePublisher.publish(
          ClientCacheRemoveEvent.builder()
              .clientId(client.getId().getValue().toString())
              .tenantId(tenantId.getValue())
              .build());
    } else {
      log.warn("Cannot evict cache for client without tenant info: {}", client.getId().getValue());
    }
  }

  /**
   * Handles client deleted events by evicting the client from the cache and publishing a removal
   * event for multi-region cache invalidation.
   *
   * @param event The client deleted event.
   */
  @TransactionalEventListener
  public void handleClientDeleted(ClientDeletedEvent event) {
    log.debug(
        "Handling client deleted event for client ID: {}", event.getClient().getId().getValue());
    var client = event.getClient();
    var tenantId =
        client.getClientTenantInfo() != null ? client.getClientTenantInfo().tenantId() : null;

    if (tenantId != null) {
      clientCacheService.evict(client.getId(), tenantId);
      clientCacheRemoveMessagePublisher.publish(
          ClientCacheRemoveEvent.builder()
              .clientId(client.getId().getValue().toString())
              .tenantId(tenantId.getValue())
              .build());
    } else {
      log.warn("Cannot evict cache for client without tenant info: {}", client.getId().getValue());
    }
  }
}
