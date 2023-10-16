/**
 *
 */
package com.onlyoffice.authorization.core.usecases.service.consent;

import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;

/**
 *
 */
public interface ConsentRetrieveUsecases {
    OAuth2AuthorizationConsent findById(String registeredClientId, String principalName);
}
