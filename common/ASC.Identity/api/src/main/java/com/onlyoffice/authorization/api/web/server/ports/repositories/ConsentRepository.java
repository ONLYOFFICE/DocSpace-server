/**
 *
 */
package com.onlyoffice.authorization.api.web.server.ports.repositories;

import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceRetrieveUsecases;
import org.springframework.data.repository.CrudRepository;

/**
 *
 */
public interface ConsentRepository extends CrudRepository<Consent, Consent.ConsentId>,
        ConsentPersistenceCreationUsecases, ConsentPersistenceRetrieveUsecases,
        ConsentPersistenceCleanupUsecases {
    /**
     *
     * @param id
     */
    void deleteById(Consent.ConsentId id);

    /**
     *
     * @param entity
     * @return
     */
    default Consent saveConsent(Consent entity) {
        return this.save(entity);
    }
}
