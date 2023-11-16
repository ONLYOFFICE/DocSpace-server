/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;

/**
 *
 */
public interface ConsentPersistenceMutationUsecases {
    Consent saveConsent(Consent entity);
    void deleteById(Consent.ConsentId id);
    void deleteAll(Iterable<? extends Consent> entities);
}
