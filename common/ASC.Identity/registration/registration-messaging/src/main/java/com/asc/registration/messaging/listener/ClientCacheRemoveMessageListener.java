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

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.service.transfer.message.ClientCacheRemoveEvent;
import com.asc.common.service.transfer.message.ClientCacheTenantRemoveEvent;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitHandler;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Profile;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Component;

/**
 * ClientCacheRemoveMessageListener listens for client cache removal events from RabbitMQ and
 * processes them.
 *
 * <p>This listener receives cache invalidation messages from the regional queue and invokes the
 * appropriate cache eviction methods. It handles both single client removal and tenant-wide removal
 * events using separate handlers for each event type.
 *
 * <p>This listener is only loaded when RabbitMQ client classes are available on the classpath.
 */
@Slf4j
@Component
@Profile("saas")
@RequiredArgsConstructor
@RabbitListener(
    queues = "asc_identity_client_cache_${spring.application.region}_queue",
    containerFactory = "rabbitListenerContainerFactory")
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class ClientCacheRemoveMessageListener {
  private final ClientCacheService clientCacheService;

  /**
   * Receives and processes single client cache removal events from RabbitMQ.
   *
   * <p>This method processes individual messages from the regional queue and evicts the specified
   * client from the cache.
   *
   * @param event The single client cache removal event.
   */
  @RabbitHandler
  public void receiveClientCacheRemoveMessage(@Payload ClientCacheRemoveEvent event) {
    log.debug("Processing single client cache removal event");

    if (event.getClientId() == null || event.getClientId().isEmpty()) {
      log.warn("Received client cache removal event with null or empty client ID");
      return;
    }

    if (event.getTenantId() == null) {
      log.warn("Received client cache removal event with null tenant ID");
      return;
    }

    clientCacheService.evict(
        new ClientId(UUID.fromString(event.getClientId())), new TenantId(event.getTenantId()));
  }

  /**
   * Receives and processes tenant-wide client cache removal events from RabbitMQ.
   *
   * <p>This method processes individual messages from the regional queue and evicts all clients for
   * the specified tenant from the cache.
   *
   * @param event The tenant-wide client cache removal event.
   */
  @RabbitHandler
  public void receiveTenantCacheRemoveMessage(@Payload ClientCacheTenantRemoveEvent event) {
    log.debug("Processing tenant-wide client cache removal event");

    if (event.getTenantId() == null) {
      log.warn("Received tenant cache removal event with null tenant ID");
      return;
    }

    clientCacheService.evictAllByTenantId(new TenantId(event.getTenantId()));
  }
}
