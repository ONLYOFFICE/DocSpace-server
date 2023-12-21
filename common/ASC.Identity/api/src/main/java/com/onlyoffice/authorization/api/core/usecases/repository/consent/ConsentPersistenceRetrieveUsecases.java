package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.Set;

/**
 *
 */
public interface ConsentPersistenceRetrieveUsecases {
    /**
     *
     * @param principalName
     * @return
     */
    @Query("SELECT c, cl FROM Consent c INNER JOIN c.client cl WHERE c.registeredClientId = cl.clientId AND c.principalName = :principalName AND cl.invalidated = false")
    Set<Consent> findAllByPrincipalName(@Param("principalName") String principalName);
}
