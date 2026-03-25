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

package com.asc.registration.data.client.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.entity.ClientEntity;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.data.client.repository.JpaClientRepository;
import java.time.ZonedDateTime;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import java.util.stream.IntStream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.EnumSource;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

class ClientQueryRepositoryDomainAdapterTest {
  @InjectMocks private ClientQueryRepositoryDomainAdapter clientQueryRepositoryDomainAdapter;
  @Mock private JpaClientRepository jpaClientRepository;
  @Mock private ClientDataAccessMapper clientDataAccessMapper;

  private ClientId clientId;
  private TenantId tenantId;
  private ClientEntity clientEntity;
  private Client client;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    clientId = new ClientId(UUID.randomUUID());
    tenantId = new TenantId(1L);
    clientEntity = mock(ClientEntity.class);
    client = mock(Client.class);

    when(clientEntity.getClientId()).thenReturn(clientId.getValue().toString());
    when(clientDataAccessMapper.toDomain(clientEntity)).thenReturn(client);
  }

  enum OptionalClientLookupKind {
    BY_ID_AND_VISIBILITY,
    BY_ID,
    BY_CLIENT_ID_AND_TENANT
  }

  @ParameterizedTest
  @EnumSource(OptionalClientLookupKind.class)
  void whenClientIsFoundByLookup_thenReturnOptionalClient(OptionalClientLookupKind kind) {
    switch (kind) {
      case BY_ID_AND_VISIBILITY -> {
        when(jpaClientRepository.findByIdAndVisibility(anyString(), eq(true)))
            .thenReturn(Optional.of(clientEntity));
        var result =
            clientQueryRepositoryDomainAdapter.findByIdAndVisibility(
                clientId, ClientVisibility.PUBLIC);
        assertEquals(Optional.of(client), result);
        verify(jpaClientRepository).findByIdAndVisibility(clientId.getValue().toString(), true);
      }
      case BY_ID -> {
        when(jpaClientRepository.findById(anyString())).thenReturn(Optional.of(clientEntity));
        var result = clientQueryRepositoryDomainAdapter.findById(clientId);
        assertEquals(Optional.of(client), result);
        verify(jpaClientRepository).findById(clientId.getValue().toString());
      }
      case BY_CLIENT_ID_AND_TENANT -> {
        when(jpaClientRepository.findByClientIdAndTenantId(anyString(), anyLong()))
            .thenReturn(Optional.of(clientEntity));
        var result =
            clientQueryRepositoryDomainAdapter.findByClientIdAndTenantId(clientId, tenantId);
        assertEquals(Optional.of(client), result);
        verify(jpaClientRepository)
            .findByClientIdAndTenantId(clientId.getValue().toString(), tenantId.getValue());
      }
    }

    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @ParameterizedTest
  @ValueSource(ints = {1, 5, 10})
  void whenClientsAreQueriedByTenantId_thenReturnPaginatedResponse(int limit) {
    List<ClientEntity> entities = List.of(clientEntity);
    ZonedDateTime lastCreatedOn = ZonedDateTime.now();

    when(jpaClientRepository.findAllByTenantIdWithCursor(
            eq(tenantId.getValue()), eq(lastCreatedOn), eq(limit + 1)))
        .thenReturn(entities);

    var result =
        clientQueryRepositoryDomainAdapter.findAllByTenantId(tenantId, limit, null, lastCreatedOn);

    assertEquals(client, result.getData().iterator().next());
    verify(jpaClientRepository)
        .findAllByTenantIdWithCursor(eq(tenantId.getValue()), eq(lastCreatedOn), eq(limit + 1));
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @ParameterizedTest
  @ValueSource(ints = {1, 2})
  void whenClientsAreFoundByClientIds_thenReturnClients(int count) {
    var clientIds =
        IntStream.range(0, count).mapToObj(i -> new ClientId(UUID.randomUUID())).toList();
    var idStrings = clientIds.stream().map(i -> i.getValue().toString()).toList();

    var entities = new java.util.ArrayList<ClientEntity>();
    var expectedClients = new java.util.ArrayList<Client>();

    for (int i = 0; i < count; i++) {
      var entity = mock(ClientEntity.class);
      var domainClient = mock(Client.class);
      when(entity.getClientId()).thenReturn(clientIds.get(i).getValue().toString());
      when(clientDataAccessMapper.toDomain(entity)).thenReturn(domainClient);

      entities.add(entity);
      expectedClients.add(domainClient);
    }

    when(jpaClientRepository.findAllByClientIds(idStrings)).thenReturn(entities);

    var result = clientQueryRepositoryDomainAdapter.findAllByClientIds(clientIds);

    assertEquals(count, result.size());
    assertEquals(expectedClients, result);
    verify(jpaClientRepository).findAllByClientIds(idStrings);
    for (var entity : entities) {
      verify(clientDataAccessMapper).toDomain(entity);
    }
  }
}
