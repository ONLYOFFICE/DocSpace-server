/**
 *
 */
package com.onlyoffice.authorization.core.usecases.repositories;

import com.onlyoffice.authorization.core.entities.Consent;

/**
 *
 */
public interface ConsentPersistenceQueryUsecases {
    /**
     *
     * @param registeredClientId
     * @param principalName
     * @return
     */
    Consent getByRegisteredClientIdAndPrincipalName(String registeredClientId, String principalName);
}
