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

import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoMoreInteractions;

import com.asc.common.service.transfer.message.AuditMessage;
import java.lang.reflect.Field;
import org.apache.logging.log4j.util.Strings;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.Mockito;
import org.springframework.amqp.core.AmqpTemplate;

class RabbitAuthorizationAuditMessagePublisherTest {
  private static void setRegion(Object publisher, String region) throws Exception {
    Field regionField = publisher.getClass().getDeclaredField("region");
    regionField.setAccessible(true);
    regionField.set(publisher, region);
  }

  @ParameterizedTest
  @ValueSource(strings = {"eu", "us", "apac"})
  void whenPublishingAuditMessage_thenSendsToRegionExchange(String region) throws Exception {
    var amqpClient = Mockito.mock(AmqpTemplate.class);

    var publisher = new RabbitAuthorizationAuditMessagePublisher(amqpClient);
    setRegion(publisher, region);

    var message = AuditMessage.builder().action(9901).tenantId(10L).userId("user-1").build();

    publisher.publish(message);

    verify(amqpClient)
        .convertAndSend("asc_identity_audit_" + region + "_exchange", Strings.EMPTY, message);
    verifyNoMoreInteractions(amqpClient);
  }

  @Test
  void whenAmqpClientThrows_thenDoesNotThrow() throws Exception {
    var amqpClient = Mockito.mock(AmqpTemplate.class);

    var publisher = new RabbitAuthorizationAuditMessagePublisher(amqpClient);
    setRegion(publisher, "eu");

    var message = AuditMessage.builder().action(9901).tenantId(10L).userId("user-1").build();

    Mockito.doThrow(new RuntimeException("boom"))
        .when(amqpClient)
        .convertAndSend(Mockito.anyString(), Mockito.anyString(), Mockito.any(AuditMessage.class));

    publisher.publish(message);

    verify(amqpClient)
        .convertAndSend(Mockito.anyString(), Mockito.anyString(), Mockito.any(AuditMessage.class));
  }
}
