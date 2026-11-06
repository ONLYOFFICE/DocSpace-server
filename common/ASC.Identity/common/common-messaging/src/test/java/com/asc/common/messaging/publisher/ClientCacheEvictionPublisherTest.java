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

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoInteractions;

import com.asc.common.messaging.configuration.ClientCacheMessagingConfiguration;
import com.asc.common.service.transfer.message.ClientCacheRemoveEvent;
import java.net.ConnectException;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.amqp.AmqpConnectException;
import org.springframework.amqp.core.AmqpTemplate;

class ClientCacheEvictionPublisherTest {
  private static final String LOCAL_QUEUE =
      ClientCacheMessagingConfiguration.authorizationRegionQueue("eu");
  private static final ClientCacheRemoveEvent EVICTION =
      ClientCacheRemoveEvent.builder().clientId("client").tenantId(1L).build();

  private AmqpTemplate template;
  private RabbitBackgroundPublisher background;
  private RabbitClientCacheRemoveMessagePublisher publisher;

  @BeforeEach
  void setUp() {
    template = mock(AmqpTemplate.class);
    background = mock(RabbitBackgroundPublisher.class);
    publisher = new RabbitClientCacheRemoveMessagePublisher(template, "eu", background);
  }

  @Test
  void whenPublishingNow_thenBothSendsDoneOnCallersThread() {
    publisher.publishNow(EVICTION);

    verifyNoInteractions(background);
    verify(template)
        .convertAndSend(
            eq(ClientCacheMessagingConfiguration.ENTRY_EXCHANGE), anyString(), eq(EVICTION));
    verify(template).convertAndSend("", LOCAL_QUEUE, EVICTION);
  }

  @Test
  void givenBroadcastFails_whenPublishingNow_thenLocalEvictionStillSent() {
    doThrow(new AmqpConnectException(new ConnectException("refused")))
        .when(template)
        .convertAndSend(
            eq(ClientCacheMessagingConfiguration.ENTRY_EXCHANGE), anyString(), any(Object.class));

    publisher.publishNow(EVICTION);

    verify(template).convertAndSend("", LOCAL_QUEUE, EVICTION);
  }
}
