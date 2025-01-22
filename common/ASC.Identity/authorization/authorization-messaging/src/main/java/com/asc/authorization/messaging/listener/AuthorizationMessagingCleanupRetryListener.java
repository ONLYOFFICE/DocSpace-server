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

package com.asc.authorization.messaging.listener;

import com.asc.common.messaging.configuration.AuthorizationMessagingConfiguration;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.stereotype.Component;

/**
 * Listener component for handling retry logic for failed authorization cleanup messages.
 *
 * <p>This component listens to messages in the dead-letter queue and attempts to reprocess them up
 * to a maximum number of retries. If the retry count is exceeded, the message is logged as a
 * failure.
 */
@Slf4j
@Component
@RequiredArgsConstructor
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
