/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.authorization;

import com.onlyoffice.authorization.api.core.entities.Authorization;

/**
 *
 */
public interface AuthorizationPersistenceCreationUsecases {
    Authorization saveAuthorization(Authorization entity);
}