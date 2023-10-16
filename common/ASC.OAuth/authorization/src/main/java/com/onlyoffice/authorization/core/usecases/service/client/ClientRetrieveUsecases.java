/**
 *
 */
package com.onlyoffice.authorization.core.usecases.service.client;

import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;

/**
 *
 */
public interface ClientRetrieveUsecases {
    RegisteredClient getClientById(String id) throws RuntimeException;
    RegisteredClient getClientByClientId(String clientId) throws RuntimeException;
}
