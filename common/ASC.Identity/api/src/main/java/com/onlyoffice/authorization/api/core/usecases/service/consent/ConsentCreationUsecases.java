/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.consent;

import com.onlyoffice.authorization.api.web.server.messaging.messages.ConsentMessage;

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
