/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.repository.client;

import com.onlyoffice.authorization.api.core.entities.Client;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

import java.util.Optional;

/**
 *
 */
public interface ClientPersistenceRetrievalUsecases {
    Optional<Client> findById(String id);
    Page<Client> findAllByTenant(int tenant, Pageable pageable);
    Optional<Client> findClientByClientIdAndTenant(String clientId, int tenant);
}
