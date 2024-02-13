/**
 *
 */
package com.asc.authorization.core.usecases.repositories.key;

import com.asc.authorization.core.entities.KeyPair;

import java.util.List;
import java.util.Optional;

/**
 *
 */
public interface KeyPairPersistenceRetrievalUsecases {
    /**
     *
     * @param id
     * @return
     */
    Optional<KeyPair> findById(String id);

    /**
     *
     * @param publicKey
     * @return
     */
    Optional<KeyPair> findByPublicKey(String publicKey);

    /**
     *
     * @return
     */
    List<KeyPair> findAll();
}
