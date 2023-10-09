package com.onlyoffice.authorization.api.usecases.service.consent;

import com.onlyoffice.authorization.api.messaging.messages.ConsentMessage;

public interface ConsentCleanupUsecases {
    void deleteConsent(ConsentMessage consentMessage);
    void deleteConsents(Iterable<ConsentMessage> consents);
}
