package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;

import java.util.Set;

/**
 *
 */
public interface ConsentPersistenceRetrieveUsecases {
    /**
     *
     * @param principalName
     * @param tenantUrl
     * @return
     */
    Set<Consent> findAllByPrincipalNameAndTenantUrl(String principalName, String tenantUrl);
}
