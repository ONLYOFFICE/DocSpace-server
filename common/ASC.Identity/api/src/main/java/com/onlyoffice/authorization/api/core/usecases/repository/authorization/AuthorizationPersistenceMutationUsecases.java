/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.authorization;

import com.onlyoffice.authorization.api.core.entities.Authorization;

/**
 *
 */
public interface AuthorizationPersistenceMutationUsecases {
    Authorization saveAuthorization(Authorization entity);
    void deleteById(String id);
    int deleteAllByClientId(String clientId);
}
