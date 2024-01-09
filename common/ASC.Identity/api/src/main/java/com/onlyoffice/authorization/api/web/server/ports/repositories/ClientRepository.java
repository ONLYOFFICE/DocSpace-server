/**
 *
 */
package com.onlyoffice.authorization.api.web.server.ports.repositories;

import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceRetrievalUsecases;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.CrudRepository;
import org.springframework.data.repository.PagingAndSortingRepository;
import org.springframework.data.repository.query.Param;

import java.time.ZonedDateTime;
import java.util.Optional;

/**
 *
 */
public interface ClientRepository extends CrudRepository<Client, String>, PagingAndSortingRepository<Client, String>,
        ClientPersistenceRetrievalUsecases, ClientPersistenceCreationUsecases,
        ClientPersistenceMutationUsecases, ClientPersistenceCleanupUsecases {
    /**
     *
     * @param id
     * @return
     */
    @Query("SELECT c FROM Client c WHERE c.clientId=:id AND c.invalidated=false")
    Optional<Client> findById(@Param("id") String id);

    /**
     *
     * @param id
     * @param tenant
     * @return
     */
    int deleteByClientIdAndTenant(String id, int tenant);

    /**
     *
     * @param id
     * @param tenant
     * @return
     */
    @EntityGraph(attributePaths = {"scopes", "tenant"})
    Optional<Client> findClientByClientIdAndTenant(String id, int tenant);

    /**
     *
     * @param tenant
     * @param pageable
     * @return
     */
    Page<Client> findAllByTenant(int tenant, Pageable pageable);

    /**
     *
     * @param clientId
     * @param tenant
     * @param secret
     * @param modifiedOn
     */
    @Modifying
    @Query("UPDATE Client c SET c.clientSecret=:secret, c.modifiedOn=:modifiedOn WHERE c.clientId=:clientId AND c.tenant=:tenant")
    void regenerateClientSecretByClientId(@Param("clientId") String clientId,
                                          @Param("tenant") int tenant,
                                          @Param("secret") String secret,
                                          @Param("modifiedOn") ZonedDateTime modifiedOn);

    /**
     *
     * @param clientId
     * @param enabled
     * @param modifiedOn
     */
    @Modifying
    @Query("UPDATE Client c set c.enabled = :enabled, c.modifiedOn = :modifiedOn WHERE c.clientId=:clientId")
    void changeActivation(@Param("clientId") String clientId,
                          @Param(value = "enabled") boolean enabled,
                          @Param(value = "modifiedOn") ZonedDateTime modifiedOn);

    /**
     *
     * @param entity
     * @return
     */
    default Client saveClient(Client entity) {
        return this.save(entity);
    }
}
