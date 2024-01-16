/**
 *
 */
package com.asc.authorization.api.web.server.ports.repositories;

import com.asc.authorization.api.core.usecases.repository.consent.ConsentPersistenceCleanupUsecases;
import com.asc.authorization.api.core.entities.Consent;
import com.asc.authorization.api.core.usecases.repository.consent.ConsentPersistenceCreationUsecases;
import com.asc.authorization.api.core.usecases.repository.consent.ConsentPersistenceRetrieveUsecases;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.CrudRepository;
import org.springframework.data.repository.query.Param;

import java.util.Set;

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
     * @param principalName
     * @param tenant
     * @return
     */
    @Query("SELECT c, cl FROM Consent c INNER JOIN c.client cl WHERE c.registeredClientId = cl.clientId AND c.principalName = :principalName AND cl.tenant = :tenant AND cl.invalidated = false")
    Set<Consent> findAllConsentsByPrincipalNameAndTenant(
            @Param("principalName") String principalName,
            @Param("tenant") int tenant
    );

    /**
     *
     * @param entity
     * @return
     */
    default Consent saveConsent(Consent entity) {
        return this.save(entity);
    }

    /**
     *
     * @param tenant
     * @param principalName
     * @return
     */
    default Set<Consent> findAllByTenantAndPrincipalName(int tenant, String principalName) {
        return findAllConsentsByPrincipalNameAndTenant(principalName, tenant);
    }
}
