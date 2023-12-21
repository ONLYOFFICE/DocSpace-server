/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.client;

import java.time.ZonedDateTime;

/**
 *
 */
public interface ClientPersistenceMutationUsecases {
    /**
     *
     * @param clientId
     * @param tenant
     * @param secret
     * @param modifiedOn
     */
    void regenerateClientSecretByClientId(String clientId, int tenant,
                                          String secret, ZonedDateTime modifiedOn);

    /**
     *
     * @param clientId
     * @param enabled
     * @param modifiedOn
     */
    void changeActivation(String clientId, boolean enabled, ZonedDateTime modifiedOn);
}
