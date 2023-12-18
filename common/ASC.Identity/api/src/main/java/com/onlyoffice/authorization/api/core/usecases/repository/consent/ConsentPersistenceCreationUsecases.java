/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;

/**
 *
 */
public interface ConsentPersistenceCreationUsecases {
    Consent saveConsent(Consent entity);
}
