// (c) Copyright Ascensio System SIA 2009-2026
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

package com.asc.common.messaging.publisher;

import com.asc.common.messaging.configuration.ClientCacheMessagingConfiguration;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientCacheTenantRemoveEvent;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 * Implementation of {@link AuthorizationMessagePublisher} that publishes {@link
 * ClientCacheTenantRemoveEvent} messages to a RabbitMQ exchange.
 *
 * <p>This publisher broadcasts tenant-wide client cache removal events to the client cache
 * messaging system using the exchange defined in {@link
 * ClientCacheMessagingConfiguration#ENTRY_EXCHANGE}. Messages are distributed across all regions
 * via the fanout exchange pattern.
 *
 * <p>This implementation is only active in the SaaS profile where multi-region cache invalidation
 * is required.
 *
 * <p>This publisher is only loaded when RabbitMQ classes are available on the classpath.
 *
 * @see AuthorizationMessagePublisher
 * @see ClientCacheTenantRemoveEvent
 * @see ClientCacheMessagingConfiguration
 */
@Slf4j
@Component
@Profile("saas")
@RequiredArgsConstructor
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class RabbitClientCacheTenantRemoveMessagePublisher
    implements AuthorizationMessagePublisher<ClientCacheTenantRemoveEvent> {
  /** The AMQP client used to send messages to RabbitMQ. */
  private final AmqpTemplate amqpClient;

  /**
   * Publishes a tenant-wide client cache removal event to the client cache message exchange.
   *
   * <p>The message is sent to the entry exchange defined in {@link
   * ClientCacheMessagingConfiguration#ENTRY_EXCHANGE} with an empty routing key. The message will
   * be distributed to all regional queues via the fanout exchange for multi-region cache
   * invalidation.
   *
   * @param message the {@link ClientCacheTenantRemoveEvent} to publish
   */
  @Override
  public void publish(ClientCacheTenantRemoveEvent message) {
    log.debug("Broadcasting a tenant-wide client cache removal event: {}", message);
    amqpClient.convertAndSend(
        ClientCacheMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);
  }
}
