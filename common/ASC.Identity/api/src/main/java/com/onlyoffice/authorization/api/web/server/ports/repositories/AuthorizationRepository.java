/**
 *
 */
package com.onlyoffice.authorization.api.web.server.ports.repositories;

import com.onlyoffice.authorization.api.core.entities.Authorization;
import com.onlyoffice.authorization.api.core.usecases.repository.authorization.AuthorizationPersistenceMutationUsecases;
import org.springframework.data.repository.CrudRepository;

/**
 *
 */
public interface AuthorizationRepository extends CrudRepository<Authorization, Authorization.AuthorizationId>, AuthorizationPersistenceMutationUsecases {
    void deleteById(String id);
    default Authorization saveAuthorization(Authorization entity) {
        return this.save(entity);
    }
}
