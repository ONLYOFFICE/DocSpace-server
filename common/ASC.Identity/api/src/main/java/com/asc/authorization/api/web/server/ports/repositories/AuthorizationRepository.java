/**
 *
 */
package com.asc.authorization.api.web.server.ports.repositories;

import com.asc.authorization.api.core.usecases.repository.authorization.AuthorizationPersistenceCleanupUsecases;
import com.asc.authorization.api.core.entities.Authorization;
import com.asc.authorization.api.core.usecases.repository.authorization.AuthorizationPersistenceCreationUsecases;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.CrudRepository;
import org.springframework.data.repository.query.Param;

/**
 *
 */
public interface AuthorizationRepository extends CrudRepository<Authorization, Authorization.AuthorizationId>,
        AuthorizationPersistenceCleanupUsecases, AuthorizationPersistenceCreationUsecases {
    /**
     *
     * @param id
     */
    void deleteById(String id);

    /**
     *
     * @param registeredClientId
     * @return
     */
    @Modifying
    @Query("DELETE FROM Authorization a WHERE a.registeredClientId = :registeredClientId")
    int deleteAllByRegisteredClientId(@Param("registeredClientId") String registeredClientId);

    /**
     *
     * @param entity
     * @return
     */
    default Authorization saveAuthorization(Authorization entity) {
        return this.save(entity);
    }

    /**
     *
     * @param registeredClientId
     * @return
     */
    default int deleteAllByClientId(String registeredClientId) {
       return this.deleteAllByRegisteredClientId(registeredClientId);
    }
}
