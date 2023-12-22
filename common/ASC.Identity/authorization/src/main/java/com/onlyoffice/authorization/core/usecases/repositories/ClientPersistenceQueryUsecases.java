/**
 *
 */
package com.onlyoffice.authorization.core.usecases.repositories;

import com.onlyoffice.authorization.core.entities.Client;

/**
 *
 */
public interface ClientPersistenceQueryUsecases {
    /**
     *
     * @param id
     * @return
     */
    Client getById(String id);

    /**
     *
     * @param clientId
     * @return
     */
    Client getClientByClientId(String clientId);
}
