/**
 *
 */
package com.onlyoffice.authorization.web.server.ports.repositories;

import com.onlyoffice.authorization.core.entities.Client;
import com.onlyoffice.authorization.core.exceptions.EntityNotFoundException;
import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import org.springframework.data.repository.Repository;

import java.util.Optional;

/**
 *
 */
public interface ClientRepository extends Repository<Client, String>,
        ClientPersistenceQueryUsecases {
    Optional<Client> findById(String id);
    Optional<Client> findClientByClientId(String clientId);
    default Client getById(String id) throws EntityNotFoundException {
        return this.findById(id)
                .orElse(null);
    }
    default Client getClientByClientId(String clientId) throws RuntimeException {
        return this.findClientByClientId(clientId)
                .orElse(null);
    }
}
