package com.onlyoffice.authorization.api.core.usecases.repository.authorization;

/**
 *
 */
public interface AuthorizationPersistenceCleanupUsecases {
    /**
     *
     * @param id
     */
    void deleteById(String id);

    /**
     *
     * @param clientId
     * @return
     */
    int deleteAllByClientId(String clientId);
}
