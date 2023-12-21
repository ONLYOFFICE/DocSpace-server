package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;

/**
 *
 */
public interface ConsentPersistenceCleanupUsecases {
    /**
     *
     * @param id
     */
    void deleteById(Consent.ConsentId id);

    /**
     *
     * @param entities
     */
    void deleteAll(Iterable<? extends Consent> entities);
}
