package com.onlyoffice.authorization.api.usecases.repository.consent;

import com.onlyoffice.authorization.api.entities.Consent;

public interface ConsentPersistenceMutationUsecases {
    Consent saveConsent(Consent entity);
    void deleteById(Consent.ConsentId id);
    void deleteAll(Iterable<? extends Consent> entities);
}
