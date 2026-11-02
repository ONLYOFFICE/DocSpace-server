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

import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.inOrder;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoInteractions;
import static org.mockito.Mockito.verifyNoMoreInteractions;

import com.asc.common.messaging.configuration.AuthorizationMessagingConfiguration;
import com.asc.common.messaging.configuration.ClientCacheMessagingConfiguration;
import com.asc.common.service.transfer.message.ClientCacheRemoveEvent;
import com.asc.common.service.transfer.message.ClientCacheTenantRemoveEvent;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.AbstractExecutorService;
import java.util.concurrent.TimeUnit;
import java.util.stream.Stream;
import org.apache.logging.log4j.util.Strings;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;
import org.springframework.amqp.AmqpException;
import org.springframework.amqp.core.AmqpTemplate;

class RabbitRemoveMessagePublishersTest {
  private static class InlineExecutor extends AbstractExecutorService {
    private final java.util.function.Consumer<Runnable> sink;

    InlineExecutor(java.util.function.Consumer<Runnable> sink) {
      this.sink = sink;
    }

    @Override
    public void execute(Runnable command) {
      sink.accept(command);
    }

    @Override
    public void shutdown() {}

    @Override
    public List<Runnable> shutdownNow() {
      return List.of();
    }

    @Override
    public boolean isShutdown() {
      return false;
    }

    @Override
    public boolean isTerminated() {
      return false;
    }

    @Override
    public boolean awaitTermination(long timeout, TimeUnit unit) {
      return true;
    }
  }

  interface AuthorizationMessagePublisherAction {
    void publish(RabbitPersistableMessagePublisher publisher);
  }

  private static final RabbitBackgroundPublisher INLINE =
      new RabbitBackgroundPublisher(new InlineExecutor(Runnable::run));

  private static final String CLIENT_ID = "client-1";
  private static final long TENANT_ID = 10L;
  private static final String USER_ID = "user-1";

  private static final String REGION = "EU";
  private static final String LOCAL_AUTHORIZATION_QUEUE =
      "asc_identity_authorization_client_cache_eu_queue";

  static Stream<Arguments> authorizationPublisherCases() {
    var clientRemoved = ClientRemovedEvent.builder().clientId(CLIENT_ID).build();
    var userRemoved = UserClientsRemovedEvent.builder().userId(USER_ID).build();
    var tenantRemoved = TenantClientsRemovedEvent.builder().tenantId(TENANT_ID).build();

    return Stream.of(
        Arguments.of(
            (AuthorizationMessagingConfiguration.ENTRY_EXCHANGE),
            (AuthorizationMessagePublisherAction)
                publisher ->
                    new RabbitAuthorizationRemoveMessagePublisher(publisher, INLINE)
                        .publish(clientRemoved),
            clientRemoved),
        Arguments.of(
            (AuthorizationMessagingConfiguration.ENTRY_EXCHANGE),
            (AuthorizationMessagePublisherAction)
                publisher ->
                    new RabbitUserClientsRemoveMessagePublisher(publisher, INLINE)
                        .publish(userRemoved),
            userRemoved),
        Arguments.of(
            (AuthorizationMessagingConfiguration.ENTRY_EXCHANGE),
            (AuthorizationMessagePublisherAction)
                publisher ->
                    new RabbitTenantClientsRemoveMessagePublisher(publisher, INLINE)
                        .publish(tenantRemoved),
            tenantRemoved));
  }

  @ParameterizedTest
  @MethodSource("authorizationPublisherCases")
  void whenAuthorizationRemovalPublished_thenSendsReliablyNowAndForDelayedSecondRun(
      String expectedExchange, AuthorizationMessagePublisherAction action, Object message) {
    var reliable = mock(RabbitPersistableMessagePublisher.class);

    action.publish(reliable);

    var order = inOrder(reliable);
    order.verify(reliable).send(expectedExchange, Strings.EMPTY, message);
    order
        .verify(reliable)
        .send(AuthorizationMessagingConfiguration.DELAYED_ENTRY_EXCHANGE, Strings.EMPTY, message);
    verifyNoMoreInteractions(reliable);
  }

  @Test
  void whenClientCacheRemovalPublished_thenSendsToEveryRegionAndToLocalAuthorizationQueue() {
    var amqpClient = mock(AmqpTemplate.class);
    var message = ClientCacheRemoveEvent.builder().clientId(CLIENT_ID).tenantId(TENANT_ID).build();

    new RabbitClientCacheRemoveMessagePublisher(amqpClient, REGION, INLINE).publish(message);

    var order = inOrder(amqpClient);
    order
        .verify(amqpClient)
        .convertAndSend(ClientCacheMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);
    order.verify(amqpClient).convertAndSend(Strings.EMPTY, LOCAL_AUTHORIZATION_QUEUE, message);
    verifyNoMoreInteractions(amqpClient);
  }

  @Test
  void
      whenClientCacheTenantRemovalPublished_thenSendsToEveryRegionAndToLocalAuthorizationQueue() {
    var amqpClient = mock(AmqpTemplate.class);
    var message = ClientCacheTenantRemoveEvent.builder().tenantId(TENANT_ID).build();

    new RabbitClientCacheTenantRemoveMessagePublisher(amqpClient, REGION, INLINE).publish(message);

    var order = inOrder(amqpClient);
    order
        .verify(amqpClient)
        .convertAndSend(ClientCacheMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);
    order.verify(amqpClient).convertAndSend(Strings.EMPTY, LOCAL_AUTHORIZATION_QUEUE, message);
    verifyNoMoreInteractions(amqpClient);
  }

  @Test
  void givenDirectSendFails_whenEvictionPublished_thenPublishStillSucceeds() {
    var amqpClient = mock(AmqpTemplate.class);
    var message = ClientCacheRemoveEvent.builder().clientId(CLIENT_ID).tenantId(TENANT_ID).build();
    doThrow(new AmqpException("queue gone"))
        .when(amqpClient)
        .convertAndSend(Strings.EMPTY, LOCAL_AUTHORIZATION_QUEUE, message);

    assertDoesNotThrow(
        () ->
            new RabbitClientCacheRemoveMessagePublisher(amqpClient, REGION, INLINE)
                .publish(message));
  }

  @Test
  void givenDelayedSendFails_whenEvictionPublished_thenCallerNotFailed() {
    var amqpClient = mock(AmqpTemplate.class);
    var message = ClientCacheRemoveEvent.builder().clientId(CLIENT_ID).tenantId(TENANT_ID).build();
    doThrow(new AmqpException("broker down"))
        .when(amqpClient)
        .convertAndSend(ClientCacheMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);

    assertDoesNotThrow(
        () ->
            new RabbitClientCacheRemoveMessagePublisher(amqpClient, REGION, INLINE)
                .publish(message));
  }

  @Test
  void whenCleanupPublished_thenCallerReturnsBeforeAnythingSent() {
    var reliable = mock(RabbitPersistableMessagePublisher.class);
    var held = new ArrayList<Runnable>();
    var background = new RabbitBackgroundPublisher(new InlineExecutor(held::add));

    new RabbitAuthorizationRemoveMessagePublisher(reliable, background)
        .publish(ClientRemovedEvent.builder().clientId(CLIENT_ID).build());

    verifyNoInteractions(reliable);
    held.forEach(Runnable::run);
    verify(reliable, times(2)).send(anyString(), anyString(), any());
  }
}
