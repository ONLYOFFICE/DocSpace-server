package com.asc.registration.service;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.verify;

import com.asc.common.core.domain.value.ClientId;
import com.asc.registration.service.ports.output.repository.ConsentCommandRepository;
import com.asc.registration.service.transfer.request.update.RevokeClientConsentCommand;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class ConsentUpdateCommandHandlerTest {
  @InjectMocks private ConsentUpdateCommandHandler consentUpdateCommandHandler;
  @Mock private ConsentCommandRepository consentCommandRepository;

  private RevokeClientConsentCommand command;

  @BeforeEach
  void setUp() {
    command = new RevokeClientConsentCommand(1, UUID.randomUUID().toString(), "user@example.com");
  }

  @Test
  void revokeConsent() {
    doNothing()
        .when(consentCommandRepository)
        .revokeConsent(any(ClientId.class), any(String.class));
    consentUpdateCommandHandler.revokeConsent(command);
    verify(consentCommandRepository).revokeConsent(any(ClientId.class), eq("user@example.com"));
  }
}
