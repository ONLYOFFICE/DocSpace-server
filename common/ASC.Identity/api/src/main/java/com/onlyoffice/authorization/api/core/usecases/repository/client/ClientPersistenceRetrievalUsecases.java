/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.client;

import com.onlyoffice.authorization.api.core.entities.Client;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

import java.util.Optional;
import java.util.Set;

/**
 *
 */
public interface ClientPersistenceRetrievalUsecases {
    /**
     *
     * @param id
     * @return
     */
    Optional<Client> findById(String id);

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
     * @return
     */
    Optional<Client> findClientByClientIdAndTenant(String clientId, int tenant);

    /**
     *
     * @param clientIds
     * @return
     */
    Set<Client> findClientsByClientIdIn(Iterable<String> clientIds);
}
