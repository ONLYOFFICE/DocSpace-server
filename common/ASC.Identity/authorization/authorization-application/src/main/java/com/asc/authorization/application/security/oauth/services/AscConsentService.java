package com.asc.authorization.application.security.oauth.services;

import com.asc.authorization.application.mapper.ConsentMapper;
import com.asc.common.data.consent.entity.ConsentEntity;
import com.asc.common.data.consent.repository.JpaConsentRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsentService;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Service to handle OAuth2 Authorization Consent operations including saving, removing, and finding
 * consents.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class AscConsentService implements OAuth2AuthorizationConsentService {
  private final JpaConsentRepository jpaConsentRepository;
  private final ConsentMapper consentMapper;

  /**
   * Saves the given OAuth2AuthorizationConsent object.
   *
   * @param authorizationConsent the OAuth2AuthorizationConsent object to save.
   */
  @Transactional(timeout = 2)
  public void save(OAuth2AuthorizationConsent authorizationConsent) {
    try {
      MDC.put("client_id", authorizationConsent.getRegisteredClientId());
      MDC.put("principal_name", authorizationConsent.getPrincipalName());
      log.info("Saving an authorization consent");
      jpaConsentRepository.save(consentMapper.toEntity(authorizationConsent));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Removes the given OAuth2AuthorizationConsent object by its registered client ID and principal
   * name.
   *
   * @param authorizationConsent the OAuth2AuthorizationConsent object to remove.
   */
  @Transactional(timeout = 2)
  public void remove(OAuth2AuthorizationConsent authorizationConsent) {
    try {
      MDC.put("client_id", authorizationConsent.getRegisteredClientId());
      MDC.put("principal_name", authorizationConsent.getPrincipalName());
      log.info("Removing an authorization consent");

      jpaConsentRepository.deleteById(
          new ConsentEntity.ConsentId(
              authorizationConsent.getRegisteredClientId(),
              authorizationConsent.getPrincipalName()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Finds an OAuth2AuthorizationConsent object by its registered client ID and principal name.
   *
   * @param registeredClientId the registered client ID.
   * @param principalName the principal name.
   * @return the found OAuth2AuthorizationConsent object, or null if not found.
   */
  public OAuth2AuthorizationConsent findById(String registeredClientId, String principalName) {
    return jpaConsentRepository
        .findById(new ConsentEntity.ConsentId(registeredClientId, principalName))
        .map(consentMapper::toConsent)
        .orElse(null);
  }
}
