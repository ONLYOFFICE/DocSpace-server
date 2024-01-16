/**
 *
 */
package com.asc.authorization.api.core.usecases.repository.client;

import java.time.ZonedDateTime;

/**
 *
 */
public interface ClientPersistenceMutationUsecases {
    /**
     *
     * @param tenant
     * @param clientId
     * @param secret
     * @param modifiedOn
     */
    void regenerateClientSecretByClientId(int tenant, String clientId,
                                          String secret, ZonedDateTime modifiedOn);

    /**
     *
     * @param tenant
     * @param clientId
     * @param enabled
     * @param modifiedOn
     */
    void changeActivation(int tenant, String clientId,
                          boolean enabled, ZonedDateTime modifiedOn);
}
