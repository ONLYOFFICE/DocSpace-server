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

package com.asc.registration.messaging.listener;

import static org.mockito.ArgumentMatchers.anySet;
import static org.mockito.Mockito.lenient;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.messaging.mapper.RabbitAuditDataMapper;
import com.asc.common.service.AuditCreateCommandHandler;
import com.asc.common.service.transfer.message.AuditMessage;
import com.rabbitmq.client.Channel;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;
import java.util.stream.IntStream;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.extension.ExtendWith;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.messaging.Message;

@ExtendWith(MockitoExtension.class)
public class AuditMessageListenerTest {
  @InjectMocks private AuditMessageListener listener;
  @Mock private AuditCreateCommandHandler auditCreateCommandHandler;
  @Mock private RabbitAuditDataMapper auditDataMapper;
  @Mock private Audit audit;
  @Mock private AuditMessage auditMessage;
  @Mock private Message<AuditMessage> message;
  @Mock private Channel channel;

  @BeforeEach
  void setUp() {
    lenient().when(message.getPayload()).thenReturn(auditMessage);
    lenient().when(auditDataMapper.toAudit(auditMessage)).thenReturn(audit);
  }

  @ParameterizedTest
  @ValueSource(ints = {0, 1, 2, 3, 5})
  void whenMessagesAreReceived_thenAuditsAreCreated(int messageCount) {
    List<Message<AuditMessage>> messages =
        IntStream.range(0, messageCount).mapToObj(i -> message).collect(Collectors.toList());

    listener.receiveMessage(messages, channel);

    if (messageCount == 0) {
      verify(auditCreateCommandHandler, never()).createAudits(anySet());
      return;
    }

    var captor = ArgumentCaptor.forClass(Set.class);
    verify(auditCreateCommandHandler, times(1)).createAudits(captor.capture());
    Assertions.assertEquals(1, captor.getValue().size());
  }
}
