package com.asc.registration.data.consent.adapter;

import com.asc.common.core.domain.exception.ConsentNotFoundException;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.data.consent.entity.ConsentEntity;
import com.asc.common.data.consent.repository.JpaConsentRepository;
import com.asc.registration.service.ports.output.repository.ConsentCommandRepository;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Repository;

/**
 * Adapter class for handling consent command operations. Implements the {@link
 * ConsentCommandRepository} interface.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class ConsentCommandRepositoryAdapter implements ConsentCommandRepository {
  private final JpaConsentRepository jpaConsentRepository;

  /**
   * Revokes a user's consent for a specific client by marking it as invalidated.
   *
   * @param clientId the client ID
   * @param principalId the principal (user) ID
   */
  public void revokeConsent(ClientId clientId, String principalId) {
    log.debug("Persisting user's consent for current client as invalidated");

    jpaConsentRepository
        .findById(new ConsentEntity.ConsentId(clientId.getValue().toString(), principalId))
        .ifPresentOrElse(
            entity -> {
              entity.setInvalidated(true);
              entity.setModifiedAt(ZonedDateTime.now(ZoneId.of("UTC")));
              jpaConsentRepository.save(entity);
            },
            () -> {
              throw new ConsentNotFoundException(
                  String.format(
                      "User %s consent for client %s was not found",
                      principalId, clientId.getValue().toString()));
            });
  }
}
