// (c) Copyright Ascensio System SIA 2009-2025
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

package com.asc.registration.data.client.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.data.client.entity.ClientEntity;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.data.client.repository.JpaClientRepository;
import java.time.ZonedDateTime;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

class ClientCommandRepositoryDomainAdapterTest {
  @InjectMocks private ClientCommandRepositoryDomainAdapter clientCommandRepositoryDomainAdapter;
  @Mock private JpaClientRepository jpaClientRepository;
  @Mock private ClientDataAccessMapper clientDataAccessMapper;
  @Mock private AuthorizationMessagePublisher<ClientRemovedEvent> authorizationMessagePublisher;
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

    // Stub publish methods
    doNothing().when(messagePublisher).publish(any(ClientEvent.class));
    doNothing().when(authorizationMessagePublisher).publish(any(ClientRemovedEvent.class));
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

  @Test
  void whenVisibilityIsChanged_thenRepositoryIsUpdated() {
    clientCommandRepositoryDomainAdapter.changeVisibilityByTenantIdAndClientId(
        mock(ClientEvent.class), tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeVisibility(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
    verify(messagePublisher).publish(any(ClientEvent.class));
  }

  @Test
  void whenActivationIsChanged_thenRepositoryIsUpdated() {
    clientCommandRepositoryDomainAdapter.changeActivationByTenantIdAndClientId(
        mock(ClientEvent.class), tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeActivation(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
    verify(messagePublisher).publish(any(ClientEvent.class));
  }

  @Test
  void whenClientIsDeleted_thenRepositoryAndPublishersAreCalled() {
    when(jpaClientRepository.deleteByClientIdAndTenantId(anyString(), anyInt())).thenReturn(0);

    var result =
        clientCommandRepositoryDomainAdapter.deleteByTenantIdAndClientId(
            mock(ClientEvent.class), tenantId, clientId);

    verify(jpaClientRepository)
        .deleteByClientIdAndTenantId(eq(clientId.getValue().toString()), eq(tenantId.getValue()));
    verify(messagePublisher).publish(any(ClientEvent.class));
    verify(authorizationMessagePublisher).publish(any(ClientRemovedEvent.class));

    assertEquals(0, result);
  }
}
