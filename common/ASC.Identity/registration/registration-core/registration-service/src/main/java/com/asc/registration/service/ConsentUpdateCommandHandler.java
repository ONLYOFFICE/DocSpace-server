package com.asc.registration.service;

import com.asc.common.core.domain.value.ClientId;
import com.asc.registration.service.ports.output.repository.ConsentCommandRepository;
import com.asc.registration.service.transfer.request.update.RevokeClientConsentCommand;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Isolation;
import org.springframework.transaction.annotation.Transactional;

@Slf4j
@Component
@RequiredArgsConstructor
public class ConsentUpdateCommandHandler {
  private final ConsentCommandRepository consentCommandRepository;

  /**
   * Revokes user consent for a given client.
   *
   * @param command the command containing client ID and principal name
   */
  @Transactional(timeout = 2, isolation = Isolation.REPEATABLE_READ)
  public void revokeConsent(RevokeClientConsentCommand command) {
    log.info("Trying to revoke user consent");

    consentCommandRepository.revokeConsent(
        new ClientId(UUID.fromString(command.getClientId())), command.getPrincipalName());
  }
}
