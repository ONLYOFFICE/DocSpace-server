package com.onlyoffice.authorization.api.core.usecases.repository.client;

import com.onlyoffice.authorization.api.core.entities.Client;

/**
 *
 */
public interface ClientPersistenceCreationUsecases {
    /**
     *
     * @param entity
     * @return
     */
    Client saveClient(Client entity);
}
