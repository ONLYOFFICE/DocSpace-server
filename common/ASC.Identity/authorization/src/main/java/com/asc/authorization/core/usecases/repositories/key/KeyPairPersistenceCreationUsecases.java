package com.asc.authorization.core.usecases.repositories.key;

import com.asc.authorization.core.entities.KeyPair;

/**
 *
 */
public interface KeyPairPersistenceCreationUsecases {
    /**
     *
     * @param entity
     * @return
     */
    KeyPair save(KeyPair entity);
}
