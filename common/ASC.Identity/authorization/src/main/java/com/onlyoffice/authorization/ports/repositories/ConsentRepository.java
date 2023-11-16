/**
 *
 */
package com.onlyoffice.authorization.ports.repositories;

import com.onlyoffice.authorization.core.entities.Consent;
import com.onlyoffice.authorization.core.usecases.repositories.ConsentPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.exceptions.EntityNotFoundException;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.Repository;
import org.springframework.data.repository.query.Param;

import java.util.Optional;

/**
 *
 */
public interface ConsentRepository extends Repository<Consent, Consent.ConsentId>,
        ConsentPersistenceQueryUsecases {
    @Query(value = """
            SELECT c FROM identity_consents c WHERE c.registered_client_id=:registeredClientId AND c.principal_name=:principalName AND c.invalidated != 1
            """, nativeQuery = true)
    Optional<Consent> findByRegisteredClientIdAndPrincipalName(@Param("registeredClientId") String registeredClientId,
                                                               @Param("principalName") String principalName);
    default Consent getByRegisteredClientIdAndPrincipalName(String registeredClientId, String principalName) throws RuntimeException {
        return this.findByRegisteredClientIdAndPrincipalName(registeredClientId, principalName)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find consent with client id and principal name",
                                registeredClientId, principalName)));
    }
}
