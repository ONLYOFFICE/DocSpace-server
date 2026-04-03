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
