package com.asc.authorization.core.usecases.service.key;

/**
 *
 */
public interface KeyPairCleanupUsecases {
    /**
     *
     * @param id
     */
    void deleteById(String id);

    /**
     *
     * @param publicKey
     */
    void deleteByPublicKey(String publicKey);
}
