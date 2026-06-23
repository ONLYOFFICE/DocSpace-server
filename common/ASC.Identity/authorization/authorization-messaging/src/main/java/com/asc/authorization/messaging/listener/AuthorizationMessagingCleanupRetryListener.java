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

import com.asc.common.messaging.configuration.AuthorizationMessagingConfiguration;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.stereotype.Component;

/**
 * Listener component for handling retry logic for failed authorization cleanup messages.
 *
 * <p>This component listens to messages in the dead-letter queue and attempts to reprocess them up
 * to a maximum number of retries. If the retry count is exceeded, the message is logged as a
 * failure.
 *
 * <p>This listener is only loaded when RabbitMQ client classes are available on the classpath.
 */
@Slf4j
@Component
@RequiredArgsConstructor
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class AuthorizationMessagingCleanupRetryListener {
  private final RabbitTemplate rabbitTemplate;

  /**
   * Listens for messages in the dead-letter queue and attempts to reprocess them.
   *
   * <p>If the retry count (from the x-death header) is less than or equal to 5, the message is
   * re-sent to the exchange for reprocessing. If the retry count exceeds 5, the message is
   * considered as failed and logged.
   *
   * @param message the incoming RabbitMQ message from the dead-letter queue.
   */
  @RabbitListener(queues = AuthorizationMessagingConfiguration.DEAD_LETTER_QUEUE)
  public void receiveMessage(Message message) {
    var death = message.getMessageProperties().getXDeathHeader();
    if (!death.isEmpty()) {
      var entry = death.getFirst();
      try {
        var counter = Integer.parseInt(entry.get("count").toString());
        if (counter <= 5) {
          rabbitTemplate.send(
              AuthorizationMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);
          log.info("Retrying message for queue: {}, attempt: {}", entry.get("queue"), counter);
        } else {
          throw new RuntimeException(
              "Exhausted number of retries for queue: " + entry.get("queue"));
        }
      } catch (Exception e) {
        log.error("Could not process cleanup for queue: {}", entry.get("queue"), e);
      }
    } else {
      log.info("Message received without x-death header. Skipping processing.");
    }
  }
}
