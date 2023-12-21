/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;

/**
 *
 */
public interface ConsentPersistenceCreationUsecases {
    /**
     *
     * @param entity
     * @return
     */
    Consent saveConsent(Consent entity);
}
