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

package com.asc.registration.data.client.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;
import static org.mockito.Mockito.anyBoolean;
import static org.mockito.Mockito.eq;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import java.util.ArrayList;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.PageRequest;

class ClientQueryRepositoryDomainAdapterTest {
  @InjectMocks private ClientQueryRepositoryDomainAdapter adapter;
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
    tenantId = new TenantId(1);
    clientEntity = mock(ClientEntity.class);
    client = mock(Client.class);
  }

  @Test
  void findByIdAndVisibility() {
    when(jpaClientRepository.findByIdAndVisibility(anyString(), anyBoolean()))
        .thenReturn(Optional.of(clientEntity));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var result = adapter.findByIdAndVisibility(clientId, ClientVisibility.PUBLIC);

    assertTrue(result.isPresent());
    assertEquals(client, result.get());
    verify(jpaClientRepository).findByIdAndVisibility(clientId.getValue().toString(), true);
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void findById() {
    when(jpaClientRepository.findById(anyString())).thenReturn(Optional.of(clientEntity));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var result = adapter.findById(clientId);

    assertTrue(result.isPresent());
    assertEquals(client, result.get());
    verify(jpaClientRepository).findById(clientId.getValue().toString());
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void findAllPublicAndPrivateByTenantId() {
    var page = new PageImpl<>(new ArrayList<>(Set.of(clientEntity)));

    when(jpaClientRepository.findAllPublicAndPrivateByTenant(anyInt(), any(PageRequest.class)))
        .thenReturn(page);
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var response = adapter.findAllPublicAndPrivateByTenantId(tenantId, 0, 10);

    assertTrue(response.getData().iterator().hasNext());
    assertEquals(response.getData().iterator().next(), client);
    verify(jpaClientRepository)
        .findAllPublicAndPrivateByTenant(eq(tenantId.getValue()), any(PageRequest.class));
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void findAllByTenantId() {
    var page = new PageImpl<>(new ArrayList<>(Set.of(clientEntity)));

    when(jpaClientRepository.findAllByTenantId(anyInt(), any(PageRequest.class))).thenReturn(page);
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var response = adapter.findAllByTenantId(tenantId, 0, 10);

    assertTrue(response.getData().iterator().hasNext());
    assertEquals(response.getData().iterator().next(), client);
    verify(jpaClientRepository).findAllByTenantId(eq(tenantId.getValue()), any(PageRequest.class));
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void findByClientIdAndTenantId() {
    when(jpaClientRepository.findClientByClientIdAndTenantId(anyString(), anyInt()))
        .thenReturn(Optional.of(clientEntity));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var result = adapter.findByClientIdAndTenantId(clientId, tenantId);

    assertTrue(result.isPresent());
    assertEquals(client, result.get());
    verify(jpaClientRepository)
        .findClientByClientIdAndTenantId(clientId.getValue().toString(), tenantId.getValue());
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }
}
