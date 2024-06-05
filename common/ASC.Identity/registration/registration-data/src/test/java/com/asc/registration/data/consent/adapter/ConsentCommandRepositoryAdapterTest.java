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
  private String principalName;
  private ConsentEntity consentEntity;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    clientId = new ClientId(UUID.randomUUID());
    principalName = "principal-name";
    consentEntity = new ConsentEntity();
    consentEntity.setRegisteredClientId(clientId.getValue().toString());
    consentEntity.setPrincipalName(principalName);
  }

  @Test
  void revokeConsent_Success() {
    when(jpaConsentRepository.findById(any(ConsentEntity.ConsentId.class)))
        .thenReturn(Optional.of(consentEntity));

    consentCommandRepositoryAdapter.revokeConsent(clientId, principalName);

    verify(jpaConsentRepository).findById(any(ConsentEntity.ConsentId.class));
    verify(jpaConsentRepository).save(any(ConsentEntity.class));
  }

  @Test
  void revokeConsent_ConsentNotFound() {
    when(jpaConsentRepository.findById(any(ConsentEntity.ConsentId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ConsentNotFoundException.class,
        () -> consentCommandRepositoryAdapter.revokeConsent(clientId, principalName));

    verify(jpaConsentRepository).findById(any(ConsentEntity.ConsentId.class));
    verify(jpaConsentRepository, never()).save(any(ConsentEntity.class));
  }
}
