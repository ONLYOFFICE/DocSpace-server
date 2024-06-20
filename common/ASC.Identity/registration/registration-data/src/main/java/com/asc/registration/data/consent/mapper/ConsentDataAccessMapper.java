package com.asc.registration.data.consent.mapper;

import com.asc.common.core.domain.entity.Consent;
import com.asc.common.core.domain.value.ConsentId;
import com.asc.common.core.domain.value.enums.ConsentStatus;
import com.asc.common.data.consent.entity.ConsentEntity;
import com.asc.common.data.scope.entity.ScopeEntity;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.entity.ClientConsent;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

/**
 * Mapper class to convert between {@link ConsentEntity} data access objects and {@link
 * ClientConsent} domain objects.
 */
@Component
@RequiredArgsConstructor
public class ConsentDataAccessMapper {

  /**
   * Converts a {@link ConsentEntity} and {@link Client} to a {@link ClientConsent} domain object.
   *
   * @param entity the data access object to convert
   * @param client the client domain object associated with the consent
   * @return the converted domain object
   */
  public ClientConsent toClientConsent(ConsentEntity entity, Client client) {
    return new ClientConsent(
        client,
        Consent.Builder.builder()
            .id(new ConsentId(entity.getRegisteredClientId(), entity.getPrincipalId()))
            .scopes(
                entity.getScopes().stream().map(ScopeEntity::getName).collect(Collectors.toSet()))
            .modifiedOn(entity.getModifiedAt())
            .status(entity.isInvalidated() ? ConsentStatus.INVALIDATED : ConsentStatus.ACTIVE)
            .build());
  }
}
