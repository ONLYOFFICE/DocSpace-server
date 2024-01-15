/**
 *
 */
package com.onlyoffice.authorization.api.web.server.ports.repositories;

import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceRetrieveUsecases;
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
     * @param tenantUrl
     * @return
     */
    @Query("SELECT c, cl FROM Consent c INNER JOIN c.client cl WHERE c.registeredClientId = cl.clientId AND c.principalName = :principalName AND cl.tenantUrl = :tenantUrl AND cl.invalidated = false")
    Set<Consent> findAllConsentsByPrincipalNameAndTenantUrl(
            @Param("principalName") String principalName,
            @Param("tenantUrl") String tenantUrl
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
     * @param principalName
     * @param tenantUrl
     * @return
     */
    default Set<Consent> findAllByPrincipalNameAndTenantUrl(String principalName, String tenantUrl) {
        return findAllConsentsByPrincipalNameAndTenantUrl(principalName, tenantUrl);
    }
}
