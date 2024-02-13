package com.asc.authorization.core.usecases.repositories.key;

/**
 *
 */
public interface KeyPairPersistenceCleanupUsecases {
    /**
     *
     * @param id
     * @return
     */
    void deleteById(String id);

    /**
     *
     * @param publicKey
     */
    void deleteByPublicKey(String publicKey);
}
