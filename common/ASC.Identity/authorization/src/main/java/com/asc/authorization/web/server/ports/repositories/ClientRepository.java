/**
 *
 */
package com.asc.authorization.web.server.ports.repositories;

import com.asc.authorization.core.entities.Client;
import com.asc.authorization.core.exceptions.EntityNotFoundException;
import com.asc.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import org.springframework.data.repository.Repository;

import java.util.Optional;

/**
 *
 */
public interface ClientRepository extends Repository<Client, String>,
        ClientPersistenceQueryUsecases {
    /**
     *
     * @param id
     * @return
     */
    Optional<Client> findById(String id);

    /**
     *
     * @param clientId
     * @return
     */
    Optional<Client> findClientByClientId(String clientId);

    /**
     *
     * @param id
     * @return
     * @throws EntityNotFoundException
     */
    default Client getById(String id) throws EntityNotFoundException {
        return findById(id)
                .orElse(null);
    }

    /**
     *
     * @param clientId
     * @return
     * @throws RuntimeException
     */
    default Client getClientByClientId(String clientId) throws RuntimeException {
        return findClientByClientId(clientId)
                .orElse(null);
    }
}
