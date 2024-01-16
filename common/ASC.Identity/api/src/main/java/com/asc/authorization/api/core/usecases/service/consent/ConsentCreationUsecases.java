/**
 *
 */
package com.asc.authorization.api.core.usecases.service.consent;

import com.asc.authorization.api.web.server.messaging.messages.ConsentMessage;

/**
 *
 */
public interface ConsentCreationUsecases {
    /**
     *
     * @param consentMessage
     */
    void saveConsent(ConsentMessage consentMessage);

    /**
     *
     * @param consents
     */
    void saveConsents(Iterable<ConsentMessage> consents);
}
