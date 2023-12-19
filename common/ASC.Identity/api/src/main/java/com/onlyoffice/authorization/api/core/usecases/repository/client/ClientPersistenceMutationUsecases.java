/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.client;

import java.time.ZonedDateTime;

/**
 *
 */
public interface ClientPersistenceMutationUsecases {
    void regenerateClientSecretByClientId(String clientId, int tenant,
                                          String secret, ZonedDateTime modifiedOn);
    void changeActivation(String clientId, boolean enabled, ZonedDateTime modifiedOn);
}
