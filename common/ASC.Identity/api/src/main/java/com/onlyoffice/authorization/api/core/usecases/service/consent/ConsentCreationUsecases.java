/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.consent;

import com.onlyoffice.authorization.api.core.transfer.messages.ConsentMessage;

/**
 *
 */
public interface ConsentCreationUsecases {
    void saveConsent(ConsentMessage consentMessage);
    void saveConsents(Iterable<ConsentMessage> consents);
}
