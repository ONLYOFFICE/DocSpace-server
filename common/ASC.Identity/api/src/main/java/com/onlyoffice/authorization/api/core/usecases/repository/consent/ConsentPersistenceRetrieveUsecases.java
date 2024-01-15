package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;

import java.util.Set;

/**
 *
 */
public interface ConsentPersistenceRetrieveUsecases {
    /**
     *
     * @param tenant
     * @param principalName
     * @return
     */
    Set<Consent> findAllByTenantAndPrincipalName(int tenant, String principalName);
}
