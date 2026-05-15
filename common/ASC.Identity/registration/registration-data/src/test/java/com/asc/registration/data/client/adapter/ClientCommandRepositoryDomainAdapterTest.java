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

package com.asc.registration.data.client.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.data.client.entity.ClientEntity;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.data.client.repository.JpaClientRepository;
import java.time.ZonedDateTime;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.EnumSource;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

class ClientCommandRepositoryDomainAdapterTest {
  @InjectMocks private ClientCommandRepositoryDomainAdapter clientCommandRepositoryDomainAdapter;
  @Mock private JpaClientRepository jpaClientRepository;
  @Mock private ClientDataAccessMapper clientDataAccessMapper;
  @Mock private DomainEventPublisher<ClientEvent> messagePublisher;

  private Client client;
  private ClientId clientId;
  private TenantId tenantId;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    clientId = new ClientId(UUID.randomUUID());
    tenantId = new TenantId(1L);
    client = mock(Client.class);

    when(client.getId()).thenReturn(clientId);
    when(clientDataAccessMapper.toEntity(any(Client.class))).thenReturn(mock(ClientEntity.class));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    doNothing().when(messagePublisher).publish(any(ClientEvent.class));
  }

  enum TenantClientMutationKind {
    VISIBILITY,
    ACTIVATION
  }

  @ParameterizedTest
  @EnumSource(TenantClientMutationKind.class)
  void whenTenantClientMutationIsApplied_thenRepositoryAndPublisherAreInvoked(
      TenantClientMutationKind kind) {
    var event = mock(ClientEvent.class);

    switch (kind) {
      case VISIBILITY -> {
        clientCommandRepositoryDomainAdapter.changeVisibilityByTenantIdAndClientId(
            event, tenantId, clientId, true);
        verify(jpaClientRepository)
            .changeVisibility(
                eq(tenantId.getValue()),
                eq(clientId.getValue().toString()),
                eq(true),
                any(ZonedDateTime.class));
      }
      case ACTIVATION -> {
        clientCommandRepositoryDomainAdapter.changeActivationByTenantIdAndClientId(
            event, tenantId, clientId, true);
        verify(jpaClientRepository)
            .changeActivation(
                eq(tenantId.getValue()),
                eq(clientId.getValue().toString()),
                eq(true),
                any(ZonedDateTime.class));
      }
    }

    verify(messagePublisher).publish(any(ClientEvent.class));
  }

  @Test
  void whenClientIsSaved_thenClientIsReturned() {
    var clientEntity = mock(ClientEntity.class);

    when(jpaClientRepository.save(any(ClientEntity.class))).thenReturn(clientEntity);

    var savedClient =
        clientCommandRepositoryDomainAdapter.saveClient(mock(ClientEvent.class), client);

    verify(clientDataAccessMapper).toEntity(client);
    verify(jpaClientRepository).save(any(ClientEntity.class));
    verify(clientDataAccessMapper).toDomain(clientEntity);
    verify(messagePublisher).publish(any(ClientEvent.class));

    assertEquals(client, savedClient);
  }

  @Test
  void whenClientSecretIsRegenerated_thenNewSecretIsReturned() {
    var secretCaptor = ArgumentCaptor.forClass(String.class);
    var newSecret =
        clientCommandRepositoryDomainAdapter.regenerateClientSecretByTenantIdAndClientId(
            mock(ClientEvent.class), tenantId, clientId);

    verify(jpaClientRepository)
        .regenerateClientSecretByClientId(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            secretCaptor.capture(),
            any(ZonedDateTime.class));
    verify(messagePublisher).publish(any(ClientEvent.class));

    assertEquals(newSecret, secretCaptor.getValue());
  }

  @ParameterizedTest
  @ValueSource(ints = {0, 5})
  void whenClientIsDeleted_thenRepositoryAndPublishersAreCalled(int deletedCount) {
    when(jpaClientRepository.deleteByClientIdAndTenantId(anyString(), anyLong()))
        .thenReturn(deletedCount);

    var result =
        clientCommandRepositoryDomainAdapter.deleteByTenantIdAndClientId(
            mock(ClientEvent.class), tenantId, clientId);

    verify(jpaClientRepository)
        .deleteByClientIdAndTenantId(eq(clientId.getValue().toString()), eq(tenantId.getValue()));
    verify(messagePublisher).publish(any(ClientEvent.class));

    assertEquals(deletedCount, result);
  }
}
