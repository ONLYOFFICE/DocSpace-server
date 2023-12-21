package com.onlyoffice.authorization.api.core.usecases.repository.client;

/**
 *
 */
public interface ClientPersistenceCleanupUsecases {
    /**
     *
     * @param id
     * @param tenant
     * @return
     */
    int deleteByClientIdAndTenant(String id, int tenant);
}
