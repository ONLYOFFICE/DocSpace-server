/**
 *
 */
package com.asc.authorization.api.core.usecases.repository.authorization;

import com.asc.authorization.api.core.entities.Authorization;

/**
 *
 */
public interface AuthorizationPersistenceCreationUsecases {
    /**
     *
     * @param entity
     * @return
     */
    Authorization saveAuthorization(Authorization entity);
}
