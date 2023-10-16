/**
 *
 */
package com.onlyoffice.authorization.ports.repositories;

import com.onlyoffice.authorization.core.entities.Consent;
import com.onlyoffice.authorization.core.usecases.repositories.ConsentPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.exceptions.EntityNotFoundException;
import org.springframework.data.repository.Repository;

import java.util.Optional;

/**
 *
 */
public interface ConsentRepository extends Repository<Consent, Consent.ConsentId>,
        ConsentPersistenceQueryUsecases {
    Optional<Consent> findByRegisteredClientIdAndPrincipalName(String registeredClientId, String principalName);
    default Consent getByRegisteredClientIdAndPrincipalName(String registeredClientId, String principalName) throws RuntimeException {
        return this.findByRegisteredClientIdAndPrincipalName(registeredClientId, principalName)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find consent with client id and principal name",
                                registeredClientId, principalName)));
    }
}
