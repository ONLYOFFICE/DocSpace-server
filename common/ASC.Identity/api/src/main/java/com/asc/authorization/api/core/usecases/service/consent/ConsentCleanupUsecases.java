/**
 *
 */
package com.asc.authorization.api.core.usecases.service.consent;

import com.asc.authorization.api.web.server.messaging.messages.ConsentMessage;

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
