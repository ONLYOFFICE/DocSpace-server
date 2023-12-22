/**
 *
 */
package com.onlyoffice.authorization.core.usecases.service.client;

import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;

/**
 *
 */
public interface ClientRetrieveUsecases {
    /**
     *
     * @param id
     * @return
     */
    RegisteredClient getClientById(String id);

    /**
     *
     * @param clientId
     * @return
     */
    RegisteredClient getClientByClientId(String clientId);
}
