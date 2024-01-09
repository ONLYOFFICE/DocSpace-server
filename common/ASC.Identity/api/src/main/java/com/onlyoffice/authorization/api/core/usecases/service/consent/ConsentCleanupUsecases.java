/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.consent;

import com.onlyoffice.authorization.api.web.server.messaging.messages.ConsentMessage;

/**
 *
 */
public interface ConsentCleanupUsecases {
    /**
     *
     * @param consentMessage
     */
    void deleteConsent(ConsentMessage consentMessage);

    /**
     *
     * @param consents
     */
    void deleteConsents(Iterable<ConsentMessage> consents);

    /**
     *
     * @param clientId
     * @param principalName
     */
    void revokeConsentAsync(String clientId, String principalName);
}
