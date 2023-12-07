/**
 *
 */
package com.onlyoffice.authorization.api.web.server.ports.repositories;

import com.onlyoffice.authorization.api.core.entities.Authorization;
import com.onlyoffice.authorization.api.core.usecases.repository.authorization.AuthorizationPersistenceMutationUsecases;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.CrudRepository;
import org.springframework.data.repository.query.Param;

/**
 *
 */
public interface AuthorizationRepository extends CrudRepository<Authorization, Authorization.AuthorizationId>, AuthorizationPersistenceMutationUsecases {
    void deleteById(String id);
    @Modifying
    @Query("DELETE FROM Authorization a WHERE a.registeredClientId=:registeredClientId")
    int deleteAllByRegisteredClientId(@Param("registeredClientId") String registeredClientId);
    default Authorization saveAuthorization(Authorization entity) {
        return this.save(entity);
    }
    default int deleteAllByClientId(String registeredClientId) {
       return this.deleteAllByRegisteredClientId(registeredClientId);
    }
}
