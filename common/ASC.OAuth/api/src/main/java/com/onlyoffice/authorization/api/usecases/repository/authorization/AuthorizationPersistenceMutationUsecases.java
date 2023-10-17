package com.onlyoffice.authorization.api.usecases.repository.authorization;

import com.onlyoffice.authorization.api.entities.Authorization;

public interface AuthorizationPersistenceMutationUsecases {
    Authorization saveAuthorization(Authorization entity);
    void deleteById(String id);
}
