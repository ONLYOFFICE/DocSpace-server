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

import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.exception.ConsentNotFoundException;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.data.consent.entity.ConsentEntity;
import com.asc.common.data.consent.repository.JpaConsentRepository;
import java.util.Optional;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

class ConsentCommandRepositoryAdapterTest {
  @InjectMocks private ConsentCommandRepositoryAdapter consentCommandRepositoryAdapter;
  @Mock private JpaConsentRepository jpaConsentRepository;

  private ClientId clientId;
  private String principalId;
  private ConsentEntity consentEntity;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    clientId = new ClientId(UUID.randomUUID());
    principalId = "principal-id";
    consentEntity = new ConsentEntity();
    consentEntity.setRegisteredClientId(clientId.getValue().toString());
    consentEntity.setPrincipalId(principalId);
  }

  @Test
  void revokeConsent_Success() {
    when(jpaConsentRepository.findById(any(ConsentEntity.ConsentId.class)))
        .thenReturn(Optional.of(consentEntity));

    consentCommandRepositoryAdapter.revokeConsent(clientId, principalId);

    verify(jpaConsentRepository).findById(any(ConsentEntity.ConsentId.class));
    verify(jpaConsentRepository).save(any(ConsentEntity.class));
  }

  @Test
  void revokeConsent_ConsentNotFound() {
    when(jpaConsentRepository.findById(any(ConsentEntity.ConsentId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ConsentNotFoundException.class,
        () -> consentCommandRepositoryAdapter.revokeConsent(clientId, principalId));

    verify(jpaConsentRepository).findById(any(ConsentEntity.ConsentId.class));
    verify(jpaConsentRepository, never()).save(any(ConsentEntity.class));
  }
}
