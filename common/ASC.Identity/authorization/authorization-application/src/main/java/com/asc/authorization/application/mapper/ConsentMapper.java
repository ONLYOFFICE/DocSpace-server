package com.asc.authorization.application.mapper;

import com.asc.common.data.consent.entity.ConsentEntity;
import com.asc.common.data.scope.entity.ScopeEntity;
import java.time.ZonedDateTime;
import java.util.stream.Collectors;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;
import org.springframework.stereotype.Component;

/**
 * Mapper class for converting between {@link ConsentEntity} and {@link OAuth2AuthorizationConsent}.
 */
@Component
public class ConsentMapper {
  /**
   * Converts an {@link OAuth2AuthorizationConsent} to a {@link ConsentEntity}.
   *
   * @param authorizationConsent the OAuth2AuthorizationConsent to convert.
   * @return the ConsentEntity.
   */
  public ConsentEntity toEntity(OAuth2AuthorizationConsent authorizationConsent) {
    return ConsentEntity.builder()
        .registeredClientId(authorizationConsent.getRegisteredClientId())
        .principalId(authorizationConsent.getPrincipalName())
        .scopes(
            authorizationConsent.getScopes().stream()
                .map(s -> ScopeEntity.builder().name(s).build())
                .collect(Collectors.toSet()))
        .modifiedAt(ZonedDateTime.now())
        .invalidated(false)
        .build();
  }

  /**
   * Converts a {@link ConsentEntity} to an {@link OAuth2AuthorizationConsent}.
   *
   * @param consent the ConsentEntity to convert.
   * @return the OAuth2AuthorizationConsent.
   */
  public OAuth2AuthorizationConsent toConsent(ConsentEntity consent) {
    var registeredClientId = consent.getRegisteredClientId();
    var builder = OAuth2AuthorizationConsent.withId(registeredClientId, consent.getPrincipalId());
    consent.getScopes().stream().map(ScopeEntity::getName).forEach(builder::scope);
    return builder.build();
  }
}
