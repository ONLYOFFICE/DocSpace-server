package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;

public interface ConsentPersistenceCleanupUsecases {
    void deleteById(Consent.ConsentId id);
    void deleteAll(Iterable<? extends Consent> entities);
}
