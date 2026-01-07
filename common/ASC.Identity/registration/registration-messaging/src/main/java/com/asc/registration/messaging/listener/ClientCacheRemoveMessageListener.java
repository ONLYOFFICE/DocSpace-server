// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
 */
@Slf4j
@Component
@Profile("saas")
@RequiredArgsConstructor
@RabbitListener(
    queues = "asc_identity_client_cache_${spring.application.region}_queue",
    containerFactory = "rabbitListenerContainerFactory")
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
