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

package com.asc.registration.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Consent;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.entity.ClientConsent;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.mapper.ConsentDataMapper;
import com.asc.registration.service.ports.output.repository.ConsentQueryRepository;
import com.asc.registration.service.transfer.request.fetch.ConsentsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.Collections;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class ConsentQueryHandlerTest {
  @InjectMocks private ConsentQueryHandler consentQueryHandler;
  @Mock private ConsentQueryRepository consentQueryRepository;
  @Mock private ClientDataMapper clientDataMapper;
  @Mock private ConsentDataMapper consentDataMapper;

  private ConsentsPaginationQuery query;
  private Client client;
  private Consent consent;
  private ClientConsent clientConsent;
  private ConsentResponse consentResponse;
  private PageableResponse<ClientConsent> pageableClientConsent;
  private PageableResponse<ConsentResponse> expectedResponse;

  @BeforeEach
  void setUp() {
    query =
        ConsentsPaginationQuery.builder().principalId("user@example.com").page(0).limit(10).build();
    client = mock(Client.class);
    consent = mock(Consent.class);
    clientConsent = new ClientConsent(client, consent);
    consentResponse = mock(ConsentResponse.class);
    var clientConsentList = Collections.singletonList(clientConsent);
    pageableClientConsent =
        PageableResponse.<ClientConsent>builder()
            .data(clientConsentList)
            .page(0)
            .limit(10)
            .next(null)
            .previous(null)
            .build();
    expectedResponse =
        PageableResponse.<ConsentResponse>builder()
            .data(Collections.singleton(consentResponse))
            .page(0)
            .limit(10)
            .next(null)
            .previous(null)
            .build();
  }

  @Test
  void getConsents() {
    when(consentQueryRepository.findAllByPrincipalId(any(String.class), anyInt(), anyInt()))
        .thenReturn(pageableClientConsent);
    when(clientDataMapper.toClientInfoResponse(any(Client.class)))
        .thenReturn(mock(ClientInfoResponse.class));
    when(consentDataMapper.toConsentResponse(any(Consent.class), any(ClientInfoResponse.class)))
        .thenReturn(consentResponse);

    var response = consentQueryHandler.getConsents(query);

    verify(consentQueryRepository).findAllByPrincipalId(any(String.class), anyInt(), anyInt());
    verify(clientDataMapper).toClientInfoResponse(any(Client.class));
    verify(consentDataMapper).toConsentResponse(any(Consent.class), any(ClientInfoResponse.class));

    assertNotNull(response);
    assertEquals(expectedResponse.getPage(), response.getPage());
    assertEquals(expectedResponse.getLimit(), response.getLimit());
    assertEquals(expectedResponse.getNext(), response.getNext());
    assertEquals(expectedResponse.getPrevious(), response.getPrevious());
    assertEquals(expectedResponse.getData(), response.getData());
  }
}
