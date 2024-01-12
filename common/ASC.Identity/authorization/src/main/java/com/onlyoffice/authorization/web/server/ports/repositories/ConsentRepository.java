/**
 *
 */
package com.onlyoffice.authorization.web.server.ports.repositories;

import com.onlyoffice.authorization.core.entities.Consent;
import com.onlyoffice.authorization.core.exceptions.EntityNotFoundException;
import com.onlyoffice.authorization.core.usecases.repositories.ConsentPersistenceQueryUsecases;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.Repository;
import org.springframework.data.repository.query.Param;

import java.util.Optional;

/**
 *
 */
public interface ConsentRepository extends Repository<Consent, Consent.ConsentId>,
        ConsentPersistenceQueryUsecases {
    /**
     *
     * @param registeredClientId
     * @param principalName
     * @return
     */
    @Query(value = """
            SELECT * FROM identity_consents c WHERE c.registered_client_id=:registeredClientId AND c.principal_name=:principalName AND c.invalidated=0
            """, nativeQuery = true)
    Optional<Consent> findByRegisteredClientIdAndPrincipalName(@Param("registeredClientId") String registeredClientId,
                                                               @Param("principalName") String principalName);

    /**
     *
     * @param registeredClientId
     * @param principalName
     * @return
     * @throws RuntimeException
     */
    default Consent getByRegisteredClientIdAndPrincipalName(String registeredClientId, String principalName) throws RuntimeException {
        return findByRegisteredClientIdAndPrincipalName(registeredClientId, principalName)
                .orElse(null);
    }
}
