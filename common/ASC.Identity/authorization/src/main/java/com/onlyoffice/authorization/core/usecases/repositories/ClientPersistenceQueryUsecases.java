/**
 *
 */
package com.onlyoffice.authorization.core.usecases.repositories;

import com.onlyoffice.authorization.core.entities.Client;

/**
 *
 */
public interface ClientPersistenceQueryUsecases {
    Client getById(String id);
    Client getClientByClientId(String clientId);
}
