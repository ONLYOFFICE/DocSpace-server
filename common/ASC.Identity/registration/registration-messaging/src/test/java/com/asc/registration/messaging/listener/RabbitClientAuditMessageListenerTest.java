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

package com.asc.registration.messaging.listener;

import static org.mockito.ArgumentMatchers.anySet;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.messaging.mapper.AuditDataMapper;
import com.asc.common.service.AuditCreateCommandHandler;
import com.asc.common.service.transfer.message.AuditMessage;
import com.rabbitmq.client.Channel;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.messaging.Message;

@ExtendWith(MockitoExtension.class)
public class RabbitClientAuditMessageListenerTest {
  @InjectMocks private RabbitClientAuditMessageListener listener;
  @Mock private AuditCreateCommandHandler auditCreateCommandHandler;
  @Mock private AuditDataMapper auditDataMapper;
  @Mock private Audit audit;
  @Mock private AuditMessage auditMessage;
  @Mock private Message<AuditMessage> message;
  @Mock private Channel channel;

  @BeforeEach
  void setUp() {
    when(message.getPayload()).thenReturn(auditMessage);
    when(auditDataMapper.toAudit(auditMessage)).thenReturn(audit);
  }

  @Test
  void testReceiveMessage() {
    List<Message<AuditMessage>> messages = Stream.of(message).collect(Collectors.toList());
    listener.receiveMessage(messages, channel);
    verify(auditCreateCommandHandler).createAudits(anySet());
  }
}
