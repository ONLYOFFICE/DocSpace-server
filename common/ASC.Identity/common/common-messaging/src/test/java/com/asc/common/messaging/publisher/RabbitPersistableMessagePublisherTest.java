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

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

import ch.qos.logback.classic.Level;
import ch.qos.logback.classic.Logger;
import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.core.read.ListAppender;
import com.asc.common.service.ports.output.repository.PendingMessageRepository;
import com.asc.common.service.ports.output.repository.PendingMessageRepository.PendingMessage;
import java.net.ConnectException;
import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.AmqpConnectException;
import org.springframework.amqp.AmqpException;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.connection.CorrelationData;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.SimpleMessageConverter;

class RabbitPersistableMessagePublisherTest {
  private static final String EXCHANGE = "exchange";
  private static final Duration BUDGET = Duration.ofSeconds(40);

  private RabbitTemplate rabbitTemplate;
  private PendingMessageRepository store;
  private RabbitPersistableMessagePublisher publisher;

  @BeforeEach
  void setUp() {
    rabbitTemplate = mock(RabbitTemplate.class);
    store = mock(PendingMessageRepository.class);

    var connectionFactory = mock(ConnectionFactory.class);

    when(connectionFactory.isPublisherConfirms()).thenReturn(true);
    when(rabbitTemplate.getConnectionFactory()).thenReturn(connectionFactory);
    when(rabbitTemplate.getMessageConverter()).thenReturn(new SimpleMessageConverter());

    publisher = new RabbitPersistableMessagePublisher(rabbitTemplate, store);
  }

  private void brokerConfirms(boolean ack) {
    doAnswer(
            invocation -> {
              invocation
                  .getArgument(3, CorrelationData.class)
                  .getFuture()
                  .complete(new CorrelationData.Confirm(ack, ack ? null : "nacked"));
              return null;
            })
        .when(rabbitTemplate)
        .send(anyString(), anyString(), any(Message.class), any(CorrelationData.class));
  }

  private void brokerUnreachable() {
    doThrow(new AmqpConnectException(new ConnectException("refused")))
        .when(rabbitTemplate)
        .send(anyString(), anyString(), any(Message.class), any(CorrelationData.class));
  }

  @Test
  void givenBrokerConfirms_whenSending_thenNothingKept() {
    brokerConfirms(true);

    publisher.send(EXCHANGE, "", "payload");

    verify(store, never()).save(anyString(), anyString(), anyString(), any());
  }

  @Test
  void givenBrokerRefuses_whenSending_thenMessageKept() {
    brokerConfirms(false);

    publisher.send(EXCHANGE, "", "payload");

    verify(store).save(eq(EXCHANGE), eq(""), anyString(), eq("payload".getBytes()));
  }

  @Test
  void givenBrokerUnreachable_whenSending_thenMessageKept() {
    brokerUnreachable();

    publisher.send(EXCHANGE, "", "payload");

    verify(store).save(eq(EXCHANGE), eq(""), anyString(), eq("payload".getBytes()));
  }

  @Test
  void givenBrokerAndStoreUnavailable_whenSending_thenFailurePropagates() {
    brokerUnreachable();
    doThrow(new IllegalStateException("database is down"))
        .when(store)
        .save(anyString(), anyString(), anyString(), any());

    assertThrows(IllegalStateException.class, () -> publisher.send(EXCHANGE, "", "payload"));
  }

  @Test
  void givenKeptMessage_whenRelayed_thenSentWithOriginalPropertiesAndRemoved() {
    brokerUnreachable();

    publisher.send(EXCHANGE, "key", "payload");

    var headers = ArgumentCaptor.forClass(String.class);

    verify(store).save(eq(EXCHANGE), eq("key"), headers.capture(), any());

    var original = new SimpleMessageConverter().toMessage("payload", new MessageProperties());

    reset(rabbitTemplate);

    var connectionFactory = mock(ConnectionFactory.class);

    when(connectionFactory.isPublisherConfirms()).thenReturn(true);
    when(rabbitTemplate.getConnectionFactory()).thenReturn(connectionFactory);

    brokerConfirms(true);

    when(store.findDue(10))
        .thenReturn(
            List.of(
                new PendingMessage(
                    7, EXCHANGE, "key", headers.getValue(), "payload".getBytes(), 0, 10)));

    assertEquals(1, publisher.relayDue(10, BUDGET));

    var resent = ArgumentCaptor.forClass(Message.class);

    verify(rabbitTemplate)
        .send(eq(EXCHANGE), eq("key"), resent.capture(), any(CorrelationData.class));
    assertEquals(
        original.getMessageProperties().getContentType(),
        resent.getValue().getMessageProperties().getContentType());
    assertEquals("payload", new String(resent.getValue().getBody()));
    verify(store).delete(7);
  }

  @Test
  void givenBrokerStillDown_whenRelaying_thenFirstMessagePostponedAndRestWait() {
    brokerUnreachable();

    when(store.findDue(10))
        .thenReturn(
            List.of(
                new PendingMessage(1, EXCHANGE, "", "{}", new byte[0], 2, 10),
                new PendingMessage(2, EXCHANGE, "", "{}", new byte[0], 0, 10)));

    assertEquals(0, publisher.relayDue(10, BUDGET));

    verify(store).postpone(1, RabbitPersistableMessagePublisher.retryDelay(3).toSeconds());
    verify(store, never()).postpone(eq(2L), anyLong());
    verify(store, never()).delete(anyLong());
    verify(rabbitTemplate, times(1))
        .send(anyString(), anyString(), any(Message.class), any(CorrelationData.class));
  }

  @Test
  void whenComputingRetryDelays_thenTheyDoubleUpToMaximum() {
    assertEquals(Duration.ofSeconds(30), RabbitPersistableMessagePublisher.retryDelay(1));
    assertEquals(Duration.ofSeconds(60), RabbitPersistableMessagePublisher.retryDelay(2));
    assertEquals(Duration.ofSeconds(480), RabbitPersistableMessagePublisher.retryDelay(5));
    assertEquals(Duration.ofMinutes(10), RabbitPersistableMessagePublisher.retryDelay(6));
    assertEquals(Duration.ofMinutes(10), RabbitPersistableMessagePublisher.retryDelay(40));
  }

  @Test
  void givenMessageKeptTooLong_whenBrokerTakesIt_thenStillSent() {
    brokerConfirms(true);

    var tooOld = RabbitPersistableMessagePublisher.MAX_PENDING_AGE.toSeconds() + 1;

    when(store.findDue(10))
        .thenReturn(
            List.of(new PendingMessage(1, EXCHANGE, "", "{}", "payload".getBytes(), 150, tooOld)));

    assertEquals(1, publisher.relayDue(10, BUDGET));

    verify(store).delete(1);
    verify(rabbitTemplate)
        .send(eq(EXCHANGE), anyString(), any(Message.class), any(CorrelationData.class));
  }

  @Test
  void givenMessageKeptTooLong_whenRelaying_thenGivenUpAndNextOneStillSent() {
    brokerConfirms(true);
    doThrow(new AmqpException("no exchange 'gone'"))
        .when(rabbitTemplate)
        .send(eq("gone"), anyString(), any(Message.class), any(CorrelationData.class));

    var tooOld = RabbitPersistableMessagePublisher.MAX_PENDING_AGE.toSeconds() + 1;
    var body = "{\"tenant_id\":42,\"user_id\":\"user\"}";
    var logger = (Logger) LoggerFactory.getLogger(RabbitPersistableMessagePublisher.class);
    var appender = new ListAppender<ILoggingEvent>();

    appender.start();
    logger.addAppender(appender);
    try {
      when(store.findDue(10))
          .thenReturn(
              List.of(
                  new PendingMessage(1, "gone", "", "{}", body.getBytes(), 150, tooOld),
                  new PendingMessage(2, EXCHANGE, "", "{}", new byte[0], 0, 10)));

      assertEquals(1, publisher.relayDue(10, BUDGET));
    } finally {
      logger.detachAppender(appender);
    }

    verify(store).delete(1);
    verify(store).delete(2);
    verify(store, never()).postpone(anyLong(), anyLong());
    verify(rabbitTemplate)
        .send(eq(EXCHANGE), anyString(), any(Message.class), any(CorrelationData.class));
    assertTrue(
        appender.list.stream()
            .anyMatch(
                event ->
                    event.getLevel() == Level.ERROR
                        && event.getFormattedMessage().contains(body)
                        && event.getFormattedMessage().contains("gone")));
  }

  @Test
  void givenSlowConfirms_whenRelaying_thenRoundEndsWithinBudget() {
    var confirmDelayMs = 200L;
    doAnswer(
            invocation -> {
              Thread.sleep(confirmDelayMs);
              invocation
                  .getArgument(3, CorrelationData.class)
                  .getFuture()
                  .complete(new CorrelationData.Confirm(true, null));
              return null;
            })
        .when(rabbitTemplate)
        .send(anyString(), anyString(), any(Message.class), any(CorrelationData.class));

    var due = new ArrayList<PendingMessage>();
    for (long id = 1; id <= 10; id++)
      due.add(new PendingMessage(id, EXCHANGE, "", "{}", new byte[0], 0, 10));
    when(store.findDue(10)).thenReturn(due);

    var budget = Duration.ofMillis(500);
    var started = System.nanoTime();
    var sent = publisher.relayDue(10, budget);
    var took = Duration.ofNanos(System.nanoTime() - started);

    assertTrue(sent > 0 && sent < 10, "sent " + sent + " of 10");
    assertTrue(took.compareTo(budget.plusMillis(confirmDelayMs * 2)) < 0, "the round took " + took);
    verify(store, times(sent)).delete(anyLong());
  }
}
