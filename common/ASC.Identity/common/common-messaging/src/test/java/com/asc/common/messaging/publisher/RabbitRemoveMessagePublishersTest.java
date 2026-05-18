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

import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoMoreInteractions;

import com.asc.common.messaging.configuration.AuthorizationMessagingConfiguration;
import com.asc.common.messaging.configuration.ClientCacheMessagingConfiguration;
import com.asc.common.service.transfer.message.ClientCacheRemoveEvent;
import com.asc.common.service.transfer.message.ClientCacheTenantRemoveEvent;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import java.util.stream.Stream;
import org.apache.logging.log4j.util.Strings;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;
import org.springframework.amqp.core.AmqpTemplate;

class RabbitRemoveMessagePublishersTest {
  private static final String CLIENT_ID = "client-1";
  private static final long TENANT_ID = 10L;
  private static final String USER_ID = "user-1";

  static Stream<Arguments> authorizationPublisherCases() {
    var clientRemoved = ClientRemovedEvent.builder().clientId(CLIENT_ID).build();
    var userRemoved = UserClientsRemovedEvent.builder().userId(USER_ID).build();
    var tenantRemoved = TenantClientsRemovedEvent.builder().tenantId(TENANT_ID).build();

    return Stream.of(
        Arguments.of(
            (AuthorizationMessagingConfiguration.ENTRY_EXCHANGE),
            (AuthorizationMessagePublisherAction)
                publisher ->
                    new RabbitAuthorizationRemoveMessagePublisher(publisher).publish(clientRemoved),
            clientRemoved),
        Arguments.of(
            (AuthorizationMessagingConfiguration.ENTRY_EXCHANGE),
            (AuthorizationMessagePublisherAction)
                publisher ->
                    new RabbitUserClientsRemoveMessagePublisher(publisher).publish(userRemoved),
            userRemoved),
        Arguments.of(
            (AuthorizationMessagingConfiguration.ENTRY_EXCHANGE),
            (AuthorizationMessagePublisherAction)
                publisher ->
                    new RabbitTenantClientsRemoveMessagePublisher(publisher).publish(tenantRemoved),
            tenantRemoved));
  }

  @ParameterizedTest
  @MethodSource("authorizationPublisherCases")
  void whenAuthorizationRemovalPublished_thenSendsToEntryExchange(
      String expectedExchange, AuthorizationMessagePublisherAction action, Object message) {
    var amqpClient = mock(AmqpTemplate.class);

    action.publish(amqpClient);

    verify(amqpClient).convertAndSend(expectedExchange, Strings.EMPTY, message);
    verifyNoMoreInteractions(amqpClient);
  }

  interface AuthorizationMessagePublisherAction {
    void publish(AmqpTemplate amqpClient);
  }

  @Test
  void whenClientCacheRemovalPublished_thenSendsToCacheEntryExchange() {
    var amqpClient = mock(AmqpTemplate.class);
    var message = ClientCacheRemoveEvent.builder().clientId(CLIENT_ID).tenantId(TENANT_ID).build();

    new RabbitClientCacheRemoveMessagePublisher(amqpClient).publish(message);

    verify(amqpClient)
        .convertAndSend(ClientCacheMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);
    verifyNoMoreInteractions(amqpClient);
  }

  @Test
  void whenClientCacheTenantRemovalPublished_thenSendsToCacheEntryExchange() {
    var amqpClient = mock(AmqpTemplate.class);
    var message = ClientCacheTenantRemoveEvent.builder().tenantId(TENANT_ID).build();

    new RabbitClientCacheTenantRemoveMessagePublisher(amqpClient).publish(message);

    verify(amqpClient)
        .convertAndSend(ClientCacheMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);
    verifyNoMoreInteractions(amqpClient);
  }
}
