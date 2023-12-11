/**
 *
 */
package com.onlyoffice.authorization.core.usecases.repositories;

import com.onlyoffice.authorization.core.entities.Consent;

/**
 *
 */
public interface ConsentPersistenceQueryUsecases {
    Consent getByRegisteredClientIdAndPrincipalName(String registeredClientId, String principalName);
}
