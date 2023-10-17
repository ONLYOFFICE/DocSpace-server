package com.onlyoffice.authorization.api.repositories;

import com.onlyoffice.authorization.api.entities.Authorization;
import com.onlyoffice.authorization.api.usecases.repository.authorization.AuthorizationPersistenceMutationUsecases;
import org.springframework.data.repository.CrudRepository;

public interface AuthorizationRepository extends CrudRepository<Authorization, Authorization.AuthorizationId>, AuthorizationPersistenceMutationUsecases {
    void deleteById(String id);
    default Authorization saveAuthorization(Authorization entity) {
        return this.save(entity);
    }
}
