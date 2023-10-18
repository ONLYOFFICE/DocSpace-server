/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.client;

import com.onlyoffice.authorization.api.core.entities.Client;

/**
 *
 */
public interface ClientPersistenceMutationUsecases {
    Client saveClient(Client entity);
    void regenerateClientSecretByClientId(String clientId, int tenantId, String secret);
    void changeActivation(String clientId, boolean enabled);
    int deleteByClientIdAndTenant(String id, int tenantId);
}
