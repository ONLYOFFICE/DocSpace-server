/**
 *
 */
package com.asc.authorization.core.usecases.repositories;

import com.asc.authorization.core.entities.Consent;

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
