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

package com.asc.authorization.messaging.listener;

import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.Mockito.*;

import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.authorization.data.consent.repository.JpaConsentRepository;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import com.rabbitmq.client.Channel;
import java.io.IOException;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.transaction.PlatformTransactionManager;

@ExtendWith(MockitoExtension.class)
class AuthorizationMessagingCleanupListenerTest {
  @InjectMocks AuthorizationMessagingCleanupListener cleanupListener;
  @Mock private Channel channel;
  @Mock private PlatformTransactionManager transactionManager;
  @Mock private JpaAuthorizationRepository jpaAuthorizationRepository;
  @Mock private JpaConsentRepository jpaConsentRepository;

  @Test
  void whenClientRemovedEventIsSent_thenReceiveClientRemovedMessage() throws IOException {
    var event = mock(ClientRemovedEvent.class);
    when(event.getClientId()).thenReturn("client");

    cleanupListener.receiveClientRemovedMessage(event, channel, 1L);

    verify(jpaAuthorizationRepository).deleteAllAuthorizationsByClientId("client");
    verify(jpaConsentRepository).deleteAllConsentsByClientId("client");
    verify(channel).basicAck(1L, false);
    verify(channel, never()).basicNack(anyLong(), anyBoolean(), anyBoolean());
  }

  @Test
  void whenClientRemovedEvent_andRepositoryThrows_thenNacks() throws IOException {
    var event = mock(ClientRemovedEvent.class);

    when(event.getClientId()).thenReturn("client");
    doThrow(new RuntimeException("Database Exception"))
        .when(jpaAuthorizationRepository)
        .deleteAllAuthorizationsByClientId("client");

    cleanupListener.receiveClientRemovedMessage(event, channel, 1L);

    verify(channel, never()).basicAck(anyLong(), anyBoolean());
    verify(channel).basicNack(1L, false, false);
  }

  @Test
  void whenUserClientsRemovedEvent_thenDeletesAuthorizationsAndConsents() throws IOException {
    var event = mock(UserClientsRemovedEvent.class);
    when(event.getUserId()).thenReturn("user");

    cleanupListener.receiveUserClientsRemovedMessage(event, channel, 2L);

    verify(jpaAuthorizationRepository).deleteAllAuthorizationsByPrincipalId("user");
    verify(jpaConsentRepository).deleteAllConsentsByPrincipalId("user");
    verify(channel).basicAck(2L, false);
  }

  @Test
  void whenUserClientsRemovedEvent_andRepositoryThrows_thenNacks() throws IOException {
    var event = mock(UserClientsRemovedEvent.class);
    when(event.getUserId()).thenReturn("user");
    doThrow(new RuntimeException("Database Exception"))
        .when(jpaAuthorizationRepository)
        .deleteAllAuthorizationsByPrincipalId("user");

    cleanupListener.receiveUserClientsRemovedMessage(event, channel, 2L);

    verify(channel, never()).basicAck(anyLong(), anyBoolean());
    verify(channel).basicNack(2L, false, false);
  }

  @Test
  void whenTenantClientsRemovedEvent_thenDeletesAuthorizationsAndConsents() throws IOException {
    var event = mock(TenantClientsRemovedEvent.class);
    when(event.getTenantId()).thenReturn(42L);

    cleanupListener.receiveTenantClientsRemovedMessage(event, channel, 3L);

    verify(jpaConsentRepository).deleteAllConsentsByTenantId(42);
    verify(jpaAuthorizationRepository).deleteAllAuthorizationsByTenantId(42);
    verify(channel).basicAck(3L, false);
  }
}
