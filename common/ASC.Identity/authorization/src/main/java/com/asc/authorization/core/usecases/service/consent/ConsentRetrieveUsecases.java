/**
 *
 */
package com.asc.authorization.core.usecases.service.consent;

import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;

/**
 *
 */
public interface ConsentRetrieveUsecases {
    /**
     *
     * @param registeredClientId
     * @param principalName
     * @return
     */
    OAuth2AuthorizationConsent findById(String registeredClientId, String principalName);
}
