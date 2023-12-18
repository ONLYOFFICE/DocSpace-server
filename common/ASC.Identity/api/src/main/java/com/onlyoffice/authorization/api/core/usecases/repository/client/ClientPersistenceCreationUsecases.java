package com.onlyoffice.authorization.api.core.usecases.repository.client;

import com.onlyoffice.authorization.api.core.entities.Client;

public interface ClientPersistenceCreationUsecases {
    Client saveClient(Client entity);
}
