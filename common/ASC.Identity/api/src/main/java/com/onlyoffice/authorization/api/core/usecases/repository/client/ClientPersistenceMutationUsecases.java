/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.client;

import java.sql.Timestamp;

/**
 *
 */
public interface ClientPersistenceMutationUsecases {
    void regenerateClientSecretByClientId(String clientId, int tenant, String secret);
    void changeActivation(String clientId, boolean enabled, Timestamp modifiedOn);
}
