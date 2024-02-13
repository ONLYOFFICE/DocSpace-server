/**
 *
 */
package com.asc.authorization.core.usecases.service.key;

import com.asc.authorization.core.entities.KeyPair;

/**
 *
 */
public interface KeyPairCreationUsecases {
    /**
     *
     * @param keyPair
     * @return
     */
    KeyPair save(KeyPair keyPair) throws Exception;
}
