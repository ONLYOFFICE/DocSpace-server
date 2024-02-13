package com.asc.authorization.web.server.ports.repositories;

import com.asc.authorization.core.entities.KeyPair;
import com.asc.authorization.core.usecases.repositories.key.KeyPairPersistenceCleanupUsecases;
import com.asc.authorization.core.usecases.repositories.key.KeyPairPersistenceCreationUsecases;
import com.asc.authorization.core.usecases.repositories.key.KeyPairPersistenceRetrievalUsecases;
import org.springframework.data.repository.CrudRepository;

public interface KeyPairRepository extends CrudRepository<KeyPair, String>,
        KeyPairPersistenceCreationUsecases, KeyPairPersistenceCleanupUsecases, KeyPairPersistenceRetrievalUsecases {
}
