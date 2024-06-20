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
