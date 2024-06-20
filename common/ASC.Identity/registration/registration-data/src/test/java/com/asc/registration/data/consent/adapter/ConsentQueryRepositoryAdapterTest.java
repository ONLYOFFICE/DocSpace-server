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
