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

package com.asc.common.messaging.publisher;

import com.asc.common.service.ports.output.repository.PendingMessageRepository;
import com.asc.common.service.ports.output.repository.PendingMessageRepository.PendingMessage;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.concurrent.TimeUnit;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.AmqpException;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.rabbit.connection.CorrelationData;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.stereotype.Component;
import tools.jackson.core.type.TypeReference;
import tools.jackson.databind.json.JsonMapper;

/**
 * Publishes messages that must not be lost while the broker is unavailable.
 *
 * <p>Each message is sent and the broker's confirm awaited. If the broker cannot be reached, or
 * does not confirm the message, it is kept in {@link PendingMessageRepository} instead, and sends
 * it again later. In normal operation nothing is stored, so this costs one confirm round trip per
 * message and no database write. A message is lost if the broker and the database are both
 * unavailable at the same moment, in which case {@link #send} throws, or if the process dies after
 * the caller handed the message over but before the broker confirmed it or it was kept: nothing is
 * written ahead of the send. {@link AuthorizationCleanupPublisher} describes what that costs a
 * cleanup.
 */
@Slf4j
@Component
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class RabbitPersistableMessagePublisher {
  static final Duration MAX_PENDING_AGE = Duration.ofHours(24);
  static final long CONFIRM_TIMEOUT_MS = 5_000;

  private static final String CONTENT_TYPE_HEADER = "contentType";
  private static final Duration FIRST_RETRY_DELAY = Duration.ofSeconds(30);
  private static final Duration MAX_RETRY_DELAY = Duration.ofMinutes(10);
  private static final JsonMapper HEADERS_MAPPER = JsonMapper.builder().build();

  private final RabbitTemplate rabbitTemplate;
  private final PendingMessageRepository store;

  /**
   * @param rabbitTemplate the template to publish with
   * @param store the store keeping messages the broker could not take
   */
  public RabbitPersistableMessagePublisher(
      RabbitTemplate rabbitTemplate, PendingMessageRepository store) {
    this.rabbitTemplate = rabbitTemplate;
    this.store = store;
  }

  private void sendConfirmed(String exchange, String routingKey, Message message) throws Exception {
    var correlation = new CorrelationData();
    rabbitTemplate.send(exchange, routingKey, message, correlation);
    if (!rabbitTemplate.getConnectionFactory().isPublisherConfirms()) return;

    var confirm = correlation.getFuture().get(CONFIRM_TIMEOUT_MS, TimeUnit.MILLISECONDS);
    if (!confirm.ack())
      throw new AmqpException("The broker refused the message: " + confirm.reason());
  }

  private static String headersOf(Message message) {
    var properties = message.getMessageProperties();
    var headers = new LinkedHashMap<>(properties.getHeaders());
    headers.put(CONTENT_TYPE_HEADER, properties.getContentType());
    return HEADERS_MAPPER.writeValueAsString(headers);
  }

  private static Message toMessage(PendingMessage pending) {
    Map<String, Object> headers =
        HEADERS_MAPPER.readValue(pending.headers(), new TypeReference<>() {});
    var properties = new MessageProperties();
    properties.setContentType((String) headers.remove(CONTENT_TYPE_HEADER));
    headers.forEach(properties::setHeader);
    return new Message(pending.body(), properties);
  }

  /**
   * Returns how long to wait before the given attempt: doubling from the first delay up to the
   * maximum.
   *
   * @param attempt the number of the attempt that failed, starting at 1
   * @return the delay before the next attempt
   */
  static Duration retryDelay(int attempt) {
    var factor = 1L << Math.min(attempt - 1, 10);
    var delay = FIRST_RETRY_DELAY.multipliedBy(factor);
    return delay.compareTo(MAX_RETRY_DELAY) > 0 ? MAX_RETRY_DELAY : delay;
  }

  /**
   * Publishes a message, keeping it for a later attempt if the broker does not take it.
   *
   * @param exchange the exchange to publish to
   * @param routingKey the routing key to publish with
   * @param payload the message payload
   * @throws RuntimeException if the broker did not take the message and it could not be kept either
   */
  public void send(String exchange, String routingKey, Object payload) {
    var message = rabbitTemplate.getMessageConverter().toMessage(payload, new MessageProperties());
    try {
      sendConfirmed(exchange, routingKey, message);
    } catch (Exception e) {
      store.save(exchange, routingKey, headersOf(message), message.getBody());
      log.warn(
          "The broker did not take a message for exchange {}. It is kept for redelivery: {}",
          exchange,
          e.getMessage());
    }
  }

  /**
   * Sends the messages that are due again, oldest first, and stops at the first one the broker
   * still does not take, so that an unavailable broker is not hammered. A message kept for longer
   * than {@link #MAX_PENDING_AGE} gets one last attempt and is given up if that fails too, so that
   * a message the broker never takes cannot hold back the ones behind it. Every message published
   * here is safe to deliver late: cleanups scoped to a user or a tenant carry their removal moment.
   * A message given up is logged with its body, which names what was not cleaned up.
   *
   * <p>The round also stops once {@code budget} has passed, leaving the rest for the next round.
   * The messages read are not claimed, so a round must end while the lock that keeps other
   * instances from relaying is still held, or they would read and send the same messages again. A
   * message started before the budget ran out may still take up to the confirm timeout of {@value
   * #CONFIRM_TIMEOUT_MS} ms.
   *
   * @param limit the maximum number of messages to send in this round
   * @param budget how long the round may go on starting new messages
   * @return the number of messages sent
   */
  public int relayDue(int limit, Duration budget) {
    var deadline = System.nanoTime() + budget.toNanos();
    var sent = 0;
    for (var pending : store.findDue(limit)) {
      if (System.nanoTime() - deadline >= 0) {
        log.info("Relay round ran out of its {} budget; the rest waits for the next round", budget);
        break;
      }

      var expired = pending.ageSeconds() > MAX_PENDING_AGE.toSeconds();
      try {
        sendConfirmed(pending.exchange(), pending.routingKey(), toMessage(pending));
        store.delete(pending.id());
        sent++;
      } catch (Exception e) {
        if (expired) {
          log.error(
              "Giving up on pending message {} for exchange {} after {} attempts, kept longer than"
                  + " {}: {}. It was not delivered: {}",
              pending.id(),
              pending.exchange(),
              pending.attempts() + 1,
              MAX_PENDING_AGE,
              e.getMessage(),
              new String(pending.body(), StandardCharsets.UTF_8));
          store.delete(pending.id());
          continue;
        }

        store.postpone(pending.id(), retryDelay(pending.attempts() + 1).toSeconds());
        log.warn(
            "Could not redeliver pending message {} (attempt {}): {}",
            pending.id(),
            pending.attempts() + 1,
            e.getMessage());
        break;
      }
    }

    if (sent > 0) log.info("Redelivered {} pending message(s)", sent);
    return sent;
  }
}
