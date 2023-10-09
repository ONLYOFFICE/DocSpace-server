package com.onlyoffice.authorization.api.repositories;

import com.onlyoffice.authorization.api.entities.Consent;
import com.onlyoffice.authorization.api.usecases.repository.consent.ConsentPersistenceMutationUsecases;
import org.springframework.data.repository.CrudRepository;

public interface ConsentRepository extends CrudRepository<Consent, Consent.ConsentId>, ConsentPersistenceMutationUsecases {
    void deleteById(Consent.ConsentId id);
    default Consent saveConsent(Consent entity) {
        return this.save(entity);
    }
}
