package com.asc.registration.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Consent;
import com.asc.common.core.domain.value.TenantId;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.entity.ClientConsent;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.mapper.ConsentDataMapper;
import com.asc.registration.service.ports.output.repository.ConsentQueryRepository;
import com.asc.registration.service.transfer.request.fetch.TenantConsentsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientResponse;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.Collections;
import java.util.List;
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

  private TenantConsentsPaginationQuery query;
  private Client client;
  private Consent consent;
  private ClientConsent clientConsent;
  private ConsentResponse consentResponse;
  private PageableResponse<ClientConsent> pageableClientConsent;
  private PageableResponse<ConsentResponse> expectedResponse;

  @BeforeEach
  void setUp() {
    query =
        TenantConsentsPaginationQuery.builder()
            .tenantId(1)
            .principalName("user@example.com")
            .page(0)
            .limit(10)
            .build();

    client = mock(Client.class);
    consent = mock(Consent.class);
    clientConsent = new ClientConsent(client, consent);
    consentResponse = mock(ConsentResponse.class);

    List<ClientConsent> clientConsentList = Collections.singletonList(clientConsent);
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
    when(consentQueryRepository.findAllByTenantAndPrincipalName(
            any(TenantId.class), any(String.class), anyInt(), anyInt()))
        .thenReturn(pageableClientConsent);
    when(clientDataMapper.toClientResponse(any(Client.class)))
        .thenReturn(mock(ClientResponse.class));
    when(consentDataMapper.toConsentResponse(any(Consent.class), any(ClientResponse.class)))
        .thenReturn(consentResponse);

    PageableResponse<ConsentResponse> response = consentQueryHandler.getConsents(query);

    verify(consentQueryRepository)
        .findAllByTenantAndPrincipalName(
            any(TenantId.class), any(String.class), anyInt(), anyInt());
    verify(clientDataMapper).toClientResponse(any(Client.class));
    verify(consentDataMapper).toConsentResponse(any(Consent.class), any(ClientResponse.class));

    assertNotNull(response);
    assertEquals(expectedResponse.getPage(), response.getPage());
    assertEquals(expectedResponse.getLimit(), response.getLimit());
    assertEquals(expectedResponse.getNext(), response.getNext());
    assertEquals(expectedResponse.getPrevious(), response.getPrevious());
    assertEquals(expectedResponse.getData(), response.getData());
  }
}
