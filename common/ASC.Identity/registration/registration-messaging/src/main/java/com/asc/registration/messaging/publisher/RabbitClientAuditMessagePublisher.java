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

package com.asc.registration.messaging.publisher;

import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.registration.messaging.configuration.RabbitMQConfiguration;
import com.asc.registration.service.ports.output.message.publisher.ClientAuditMessagePublisher;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.stereotype.Component;

/**
 * RabbitClientAuditMessagePublisher publishes audit messages to RabbitMQ.
 *
 * <p>This class implements the {@link ClientAuditMessagePublisher} interface and uses RabbitMQ to
 * send audit messages. It logs the message publishing process and handles any exceptions that occur
 * during message sending.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class RabbitClientAuditMessagePublisher implements ClientAuditMessagePublisher {
  private final RabbitMQConfiguration configuration;
  private final AmqpTemplate amqpClient;

  /**
   * Publishes the given audit message to RabbitMQ.
   *
   * <p>This method retrieves the RabbitMQ queue configuration for the "audit" queue, and attempts
   * to send the audit message using the {@link AmqpTemplate}. If an exception occurs during message
   * sending, it logs the error along with the action, tenant ID, and user ID from the message using
   * the MDC (Mapped Diagnostic Context).
   *
   * @param message the audit message to be published
   */
  public void publish(AuditMessage message) {
    log.debug("Sending an audit message: {}", message);

    var queue = configuration.getQueues().get("audit");

    try {
      amqpClient.convertAndSend(queue.getExchange(), queue.getRouting(), message);
    } catch (Exception e) {
      MDC.put("action", String.valueOf(message.getAction()));
      MDC.put("tenant_id", String.valueOf(message.getTenantId()));
      MDC.put("user_id", message.getUserId());
      log.error("Could not send an audit message", e);
      MDC.clear();
    }
  }
}
