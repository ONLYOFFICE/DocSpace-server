package com.asc.authorization.api.core.usecases.repository.client;

import com.asc.authorization.api.core.entities.Client;

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
