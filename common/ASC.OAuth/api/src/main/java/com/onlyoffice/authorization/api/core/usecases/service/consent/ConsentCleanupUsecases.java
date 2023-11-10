/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.consent;

import com.onlyoffice.authorization.api.core.transfer.messages.ConsentMessage;

/**
 *
 */
public interface ConsentCleanupUsecases {
    void deleteConsent(ConsentMessage consentMessage);
    void deleteConsents(Iterable<ConsentMessage> consents);
    void asyncRevokeConsent(String clientId, String principalName);
}
