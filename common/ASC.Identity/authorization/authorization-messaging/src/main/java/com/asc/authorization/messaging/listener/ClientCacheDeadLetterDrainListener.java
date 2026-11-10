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

package com.asc.authorization.messaging.listener;

import com.asc.common.messaging.configuration.ClientCacheMessagingConfiguration;
import java.util.List;
import java.util.Map;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 * Drains the client cache dead-letter queue, so it cannot grow without bound.
 *
 * <p>A client cache eviction lands there when a region queue was full or its consumer gave up.
 * Replaying it later would not help: the cache entry it was meant to evict expires on its own. So
 * every dead letter is logged with where it came from and why, then dropped. The authorization
 * cleanup dead-letter queue is not drained here: {@link AuthorizationMessagingCleanupRetryListener}
 * retries those cleanups.
 *
 * <p>The client cache is distributed, and this queue declared, only in the saas profile.
 */
@Slf4j
@Component
@Profile("saas")
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class ClientCacheDeadLetterDrainListener {
  private static final String TYPE_ID_HEADER = "__TypeId__";
  private static final String DEATH_HEADER = "x-death";

  @SuppressWarnings("unchecked")
  private static Map<String, Object> firstDeath(Object header) {
    return header instanceof List<?> deaths
            && !deaths.isEmpty()
            && deaths.getFirst() instanceof Map<?, ?> first
        ? (Map<String, Object>) first
        : Map.of();
  }

  /**
   * Logs and drops a dead-lettered client cache eviction.
   *
   * @param message the dead-lettered message, unconverted so that any payload can be drained
   */
  @RabbitListener(
      queues = ClientCacheMessagingConfiguration.DEAD_LETTER_QUEUE,
      containerFactory = "rabbitRawListenerContainerFactory")
  public void drain(Message message) {
    var properties = message.getMessageProperties();
    var death = firstDeath(properties.getHeaders().get(DEATH_HEADER));
    log.error(
        "Dropping dead-lettered {} message from queue {} ({}), {} bytes",
        properties.getHeaders().getOrDefault(TYPE_ID_HEADER, "unknown"),
        death.getOrDefault("queue", properties.getConsumerQueue()),
        death.getOrDefault("reason", "unknown reason"),
        message.getBody().length);
  }
}
