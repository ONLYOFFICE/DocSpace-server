/**
 *
 */
package com.onlyoffice.authorization.core.usecases.service.client;

import com.onlyoffice.authorization.core.entities.Client;

/**
 *
 */
public interface ClientRetrieveUsecases {
    /**
     *
     * @param id
     * @return
     */
    Client getClientById(String id);

    /**
     *
     * @param clientId
     * @return
     */
    Client getClientByClientId(String clientId);
}
