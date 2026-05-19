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
import com.asc.registration.core.domain.event.ClientCreatedEvent;
import com.asc.registration.core.domain.event.ClientDeletedEvent;
import com.asc.registration.core.domain.event.ClientUpdatedEvent;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
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
public class ClientCacheEventListener {
  private final ClientCacheService clientCacheService;
  private final AuthorizationMessagePublisher<ClientCacheRemoveEvent>
      clientCacheRemoveMessagePublisher;

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
