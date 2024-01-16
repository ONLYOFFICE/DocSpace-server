package com.asc.authorization.api.core.usecases.repository.client;

/**
 *
 */
public interface ClientPersistenceCleanupUsecases {
    /**
     *
     * @param tenant
     * @param clientId
     * @return
     */
    int deleteByTenantAndClientId(int tenant, String clientId);
}
