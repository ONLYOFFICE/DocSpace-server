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
