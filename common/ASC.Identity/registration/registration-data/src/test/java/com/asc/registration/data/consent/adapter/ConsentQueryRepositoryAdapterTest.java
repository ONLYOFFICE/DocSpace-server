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

package com.asc.registration.data.consent.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.value.TenantId;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.consent.entity.ConsentEntity;
import com.asc.common.data.consent.repository.JpaConsentRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.entity.ClientConsent;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.data.consent.mapper.ConsentDataAccessMapper;
import java.util.List;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;

class ConsentQueryRepositoryAdapterTest {
  @InjectMocks private ConsentQueryRepositoryAdapter consentQueryRepositoryAdapter;
  @Mock private JpaConsentRepository jpaConsentRepository;
  @Mock private ClientDataAccessMapper clientDataAccessMapper;
  @Mock private ConsentDataAccessMapper consentDataAccessMapper;

  private ConsentEntity consentEntity;
  private ClientEntity clientEntity;
  private ClientConsent clientConsent;
  private Page<ConsentEntity> consentEntityPage;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    clientEntity = new ClientEntity();
    consentEntity = new ConsentEntity();
    consentEntity.setClient(clientEntity);
    consentEntityPage = new PageImpl<>(List.of(consentEntity), PageRequest.of(0, 10), 1);
    clientConsent = mock(ClientConsent.class);

    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(mock(Client.class));
    when(consentDataAccessMapper.toClientConsent(any(ConsentEntity.class), any(Client.class)))
        .thenReturn(clientConsent);
  }

  @Test
  void testFindAllByPrincipalName() {
    when(jpaConsentRepository.findAllConsentsByPrincipalId(anyString(), any(Pageable.class)))
        .thenReturn(consentEntityPage);

    var response = consentQueryRepositoryAdapter.findAllByPrincipalId("principalId", 0, 10);

    verify(jpaConsentRepository, times(1))
        .findAllConsentsByPrincipalId(anyString(), any(Pageable.class));
    verify(clientDataAccessMapper, times(1)).toDomain(any(ClientEntity.class));
    verify(consentDataAccessMapper, times(1))
        .toClientConsent(any(ConsentEntity.class), any(Client.class));

    assertEquals(0, response.getPage());
    assertEquals(10, response.getLimit());
    assertTrue(response.getData().iterator().hasNext());
  }

  @Test
  void testFindAllByTenantAndPrincipalName() {
    var tenantId = new TenantId(1);

    when(jpaConsentRepository.findAllConsentsByPrincipalIdAndTenant(
            anyString(), anyInt(), any(Pageable.class)))
        .thenReturn(consentEntityPage);

    var response =
        consentQueryRepositoryAdapter.findAllByTenantIdAndPrincipalId(
            tenantId, "principalId", 0, 10);

    verify(jpaConsentRepository, times(1))
        .findAllConsentsByPrincipalIdAndTenant(anyString(), anyInt(), any(Pageable.class));
    verify(clientDataAccessMapper, times(1)).toDomain(any(ClientEntity.class));
    verify(consentDataAccessMapper, times(1))
        .toClientConsent(any(ConsentEntity.class), any(Client.class));

    assertEquals(0, response.getPage());
    assertEquals(10, response.getLimit());
    assertTrue(response.getData().iterator().hasNext());
  }
}
