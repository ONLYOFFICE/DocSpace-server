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
import java.util.*;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
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

  @Test
  void whenClientIsFoundByIdAndVisibility_thenReturnClient() {
    when(jpaClientRepository.findByIdAndVisibility(anyString(), eq(true)))
        .thenReturn(Optional.of(clientEntity));

    var result =
        clientQueryRepositoryDomainAdapter.findByIdAndVisibility(clientId, ClientVisibility.PUBLIC);

    assertEquals(Optional.of(client), result);
    verify(jpaClientRepository).findByIdAndVisibility(clientId.getValue().toString(), true);
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void whenClientIsFoundById_thenReturnClient() {
    when(jpaClientRepository.findById(anyString())).thenReturn(Optional.of(clientEntity));

    var result = clientQueryRepositoryDomainAdapter.findById(clientId);

    assertEquals(Optional.of(client), result);
    verify(jpaClientRepository).findById(clientId.getValue().toString());
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void whenClientsAreQueriedForPublicAndPrivateByTenantId_thenReturnPaginatedResponse() {
    List<ClientEntity> entities = List.of(clientEntity);
    when(jpaClientRepository.findAllPublicAndPrivateByTenantWithCursor(anyLong(), any(), anyInt()))
        .thenReturn(entities);

    var result =
        clientQueryRepositoryDomainAdapter.findAllPublicAndPrivateByTenantId(
            tenantId, 10, null, ZonedDateTime.now());

    assertEquals(client, result.getData().iterator().next());
    verify(jpaClientRepository)
        .findAllPublicAndPrivateByTenantWithCursor(eq(tenantId.getValue()), any(), eq(11));
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void whenClientsAreQueriedByTenantId_thenReturnPaginatedResponse() {
    List<ClientEntity> entities = List.of(clientEntity);
    when(jpaClientRepository.findAllByTenantIdWithCursor(anyLong(), any(), anyInt()))
        .thenReturn(entities);

    var result =
        clientQueryRepositoryDomainAdapter.findAllByTenantId(
            tenantId, 10, null, ZonedDateTime.now());

    assertEquals(client, result.getData().iterator().next());
    verify(jpaClientRepository).findAllByTenantIdWithCursor(eq(tenantId.getValue()), any(), eq(11));
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void whenClientIsFoundByClientIdAndTenantId_thenReturnClient() {
    when(jpaClientRepository.findByClientIdAndTenantId(anyString(), anyLong()))
        .thenReturn(Optional.of(clientEntity));

    var result = clientQueryRepositoryDomainAdapter.findByClientIdAndTenantId(clientId, tenantId);

    assertEquals(Optional.of(client), result);
    verify(jpaClientRepository)
        .findByClientIdAndTenantId(clientId.getValue().toString(), tenantId.getValue());
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void whenClientsAreFoundByClientIds_thenReturnClients() {
    List<ClientEntity> entities = List.of(clientEntity);
    List<ClientId> clientIds = List.of(clientId);

    when(jpaClientRepository.findAllByClientIds(anyList())).thenReturn(entities);

    var result = clientQueryRepositoryDomainAdapter.findAllByClientIds(clientIds);

    assertEquals(1, result.size());
    assertEquals(client, result.get(0));
    verify(jpaClientRepository).findAllByClientIds(List.of(clientId.getValue().toString()));
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }
}
