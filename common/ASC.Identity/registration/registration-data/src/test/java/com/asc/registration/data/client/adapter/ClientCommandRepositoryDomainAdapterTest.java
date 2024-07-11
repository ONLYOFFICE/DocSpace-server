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
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
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

  private Client client;
  private ClientId clientId;
  private TenantId tenantId;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    clientId = new ClientId(UUID.randomUUID());
    tenantId = new TenantId(1);
    client = mock(Client.class);

    when(client.getId()).thenReturn(clientId);
    when(clientDataAccessMapper.toEntity(any(Client.class))).thenReturn(mock(ClientEntity.class));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);
  }

  @Test
  void saveClient() {
    var clientEntity = mock(ClientEntity.class);

    when(jpaClientRepository.save(any(ClientEntity.class))).thenReturn(clientEntity);

    var savedClient = clientCommandRepositoryDomainAdapter.saveClient(client);

    verify(clientDataAccessMapper).toEntity(client);
    verify(jpaClientRepository).save(any(ClientEntity.class));
    verify(clientDataAccessMapper).toDomain(clientEntity);

    assertEquals(client, savedClient);
  }

  @Test
  void regenerateClientSecretByTenantIdAndClientId() {
    var secretCaptor = ArgumentCaptor.forClass(String.class);
    var newSecret =
        clientCommandRepositoryDomainAdapter.regenerateClientSecretByTenantIdAndClientId(
            tenantId, clientId);

    verify(jpaClientRepository)
        .regenerateClientSecretByClientId(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            secretCaptor.capture(),
            any(ZonedDateTime.class));

    assertEquals(newSecret, secretCaptor.getValue());
  }

  @Test
  void changeVisibilityByTenantIdAndClientId() {
    clientCommandRepositoryDomainAdapter.changeVisibilityByTenantIdAndClientId(
        tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeVisibility(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
  }

  @Test
  void changeActivationByTenantIdAndClientId() {
    clientCommandRepositoryDomainAdapter.changeActivationByTenantIdAndClientId(
        tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeActivation(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
  }

  @Test
  void deleteByTenantIdAndClientId() {
    when(jpaClientRepository.deleteByClientIdAndTenantId(anyString(), anyInt())).thenReturn(1);

    var result =
        clientCommandRepositoryDomainAdapter.deleteByTenantIdAndClientId(tenantId, clientId);

    verify(jpaClientRepository)
        .deleteByClientIdAndTenantId(eq(clientId.getValue().toString()), eq(tenantId.getValue()));

    assertEquals(1, result);
  }

  @Test
  void regenerateClientSecretCorrectly() {
    var secret =
        clientCommandRepositoryDomainAdapter.regenerateClientSecretByTenantIdAndClientId(
            tenantId, clientId);

    verify(jpaClientRepository)
        .regenerateClientSecretByClientId(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(secret),
            any(ZonedDateTime.class));
  }

  @Test
  void changeVisibilityCorrectly() {
    clientCommandRepositoryDomainAdapter.changeVisibilityByTenantIdAndClientId(
        tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeVisibility(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
  }

  @Test
  void changeActivationCorrectly() {
    clientCommandRepositoryDomainAdapter.changeActivationByTenantIdAndClientId(
        tenantId, clientId, false);

    verify(jpaClientRepository)
        .changeActivation(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(false),
            any(ZonedDateTime.class));
  }

  @Test
  void deleteClient() {
    when(jpaClientRepository.deleteByClientIdAndTenantId(anyString(), anyInt())).thenReturn(0);

    var result =
        clientCommandRepositoryDomainAdapter.deleteByTenantIdAndClientId(tenantId, clientId);

    verify(jpaClientRepository)
        .deleteByClientIdAndTenantId(eq(clientId.getValue().toString()), eq(tenantId.getValue()));

    assertEquals(0, result);
  }

  @Test
  void verifySaveClientInteractions() {
    var clientEntity = mock(ClientEntity.class);

    when(jpaClientRepository.save(any(ClientEntity.class))).thenReturn(clientEntity);

    clientCommandRepositoryDomainAdapter.saveClient(client);

    verify(clientDataAccessMapper, times(1)).toEntity(any(Client.class));
    verify(jpaClientRepository, times(1)).save(any(ClientEntity.class));
    verify(clientDataAccessMapper, times(1)).toDomain(any(ClientEntity.class));
  }
}
