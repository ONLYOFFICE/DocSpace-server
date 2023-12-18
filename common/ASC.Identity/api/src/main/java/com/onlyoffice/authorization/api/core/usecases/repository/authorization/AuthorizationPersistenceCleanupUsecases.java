package com.onlyoffice.authorization.api.core.usecases.repository.authorization;

public interface AuthorizationPersistenceCleanupUsecases {
    void deleteById(String id);
    int deleteAllByClientId(String clientId);
}
