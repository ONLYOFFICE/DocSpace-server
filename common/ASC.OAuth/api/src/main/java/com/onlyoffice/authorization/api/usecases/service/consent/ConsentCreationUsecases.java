package com.onlyoffice.authorization.api.usecases.service.consent;

import com.onlyoffice.authorization.api.messaging.messages.ConsentMessage;

public interface ConsentCreationUsecases {
    void saveConsent(ConsentMessage consentMessage);
    void saveConsents(Iterable<ConsentMessage> consents);
}
