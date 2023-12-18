package com.onlyoffice.authorization.api.core.usecases.repository.client;

public interface ClientPersistenceCleanupUsecases {
    int deleteByClientIdAndTenant(String id, int tenant);
}
