package com.asc.registration.service;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.registration.service.ports.output.repository.ConsentCommandRepository;
import com.asc.registration.service.transfer.request.update.RevokeClientConsentCommand;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

public class ConsentUpdateCommandHandlerTest {
  @InjectMocks private ConsentUpdateCommandHandler consentUpdateCommandHandler;
  @Mock private ConsentCommandRepository consentCommandRepository;

  private RevokeClientConsentCommand revokeCommand;

  @BeforeEach
  public void setUp() {
    MockitoAnnotations.openMocks(this);

    revokeCommand =
        RevokeClientConsentCommand.builder()
            .clientId(UUID.randomUUID().toString())
            .principalId("user@example.com")
            .build();
  }

  @Test
  public void testRevokeConsent() {
    doNothing().when(consentCommandRepository).revokeConsent(any(ClientId.class), anyString());

    consentUpdateCommandHandler.revokeConsent(revokeCommand);

    verify(consentCommandRepository, times(1)).revokeConsent(any(ClientId.class), anyString());
  }
}
