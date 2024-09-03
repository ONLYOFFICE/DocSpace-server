// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.common.messaging.configuration;

import lombok.Getter;
import lombok.Setter;
import lombok.ToString;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;

/**
 * Configuration class for defining the properties of a RabbitMQ queue, exchange, and binding. It
 * also includes configurations for dead letter queues (DLQ), maximum message bytes, delivery
 * limits, and TTL.
 */
@Slf4j
@Getter
@Setter
@ToString
public class RabbitMQGenericQueueConfiguration {
  private String exchange;
  private String queue;
  private String routing;

  private String deadQueue;
  private String deadExchange;
  private String deadRouting;

  private int deadMaxBytes = 15000000;

  private int maxBytes = 20000000;
  private int deliveryLimit = 3;
  private int messageTTL = 100000;

  private boolean nonDurable = false;
  private boolean autoDelete = false;
  private boolean fanOut = false;

  /**
   * Validates the queue configuration to ensure all required fields are set correctly. If any
   * required field is missing or invalid, an IllegalArgumentException is thrown.
   *
   * @throws IllegalArgumentException if any required field is missing or invalid
   */
  public void validate() throws IllegalArgumentException {
    MDC.put("queue", queue);
    MDC.put("exchange", exchange);
    MDC.put("routing", routing);
    MDC.put("maxBytes", String.valueOf(maxBytes));
    MDC.put("deliveryLimit", String.valueOf(deliveryLimit));
    MDC.put("messageTTL", String.valueOf(messageTTL));
    MDC.put("nonDurable", String.valueOf(nonDurable));
    MDC.put("autoDelete", String.valueOf(autoDelete));
    MDC.put("fanOut", String.valueOf(fanOut));

    try {
      log.info("Validating generic queue configurations");

      if (queue == null || queue.isBlank())
        throw new IllegalArgumentException("Generic queue configuration must have 'queue' field");

      if (exchange == null || exchange.isBlank())
        throw new IllegalArgumentException(
            "Generic queue configuration must have 'exchange' field");

      if (routing == null || routing.isBlank())
        throw new IllegalArgumentException("Generic queue configuration must have 'routing' field");

      if (deadQueue != null && !deadQueue.isBlank()) {
        MDC.put("deadQueue", deadQueue);
        MDC.put("deadExchange", deadExchange);
        MDC.put("deadRouting", deadRouting);
        log.info("Building a DLQ, DLX, DLR for the queue");

        if (deadExchange == null || deadExchange.isBlank())
          throw new IllegalArgumentException(
              "Generic queue configuration must have 'exchange' for every declared dead letter queue");
        if (deadRouting == null || deadRouting.isBlank())
          throw new IllegalArgumentException(
              "Generic queue configuration must have 'routing' for every declared dead letter queue");
      }
    } finally {
      MDC.clear();
    }
  }
}
