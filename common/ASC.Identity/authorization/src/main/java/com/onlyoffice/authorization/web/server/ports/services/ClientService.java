/**
 *
 */
package com.onlyoffice.authorization.web.server.ports.services;

import com.onlyoffice.authorization.core.entities.Client;
import com.onlyoffice.authorization.core.exceptions.ClientPermissionException;
import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.usecases.service.client.ClientRetrieveUsecases;
import com.onlyoffice.authorization.web.security.crypto.cipher.Cipher;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Component;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientService implements ClientRetrieveUsecases {
    private final Cipher cipher;

    private final ClientPersistenceQueryUsecases clientUsecases;

    /**
     *
     * @param id
     * @return
     * @throws ClientPermissionException
     */
    @Cacheable(cacheNames = {"identityClients"}, cacheManager = "clientCacheManager")
    public Client getClientById(String id) throws ClientPermissionException {
        try {
            MDC.put("clientId", id);
            log.info("Trying to get client by id");

            var client = clientUsecases.getById(id);
            if (client == null) {
                return null;
            }

            if (!client.isEnabled()) {
                log.info("Client is disabled");
                throw new ClientPermissionException(String
                        .format("client with id %s is disabled", id));
            }

            client.setClientSecret(cipher.decrypt(client.getClientSecret()));

            return client;
        } catch (RuntimeException e) {
            throw e;
        } catch (Exception e) {
            log.error("Could not get client by id", e);
            return null;
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param clientId
     * @return
     * @throws ClientPermissionException
     */
    @Cacheable(cacheNames = {"identityClients"}, cacheManager = "clientCacheManager")
    public Client getClientByClientId(String clientId) throws ClientPermissionException {
        try {
            MDC.put("clientId", clientId);
            log.info("Trying to get client by client id");

            var client = clientUsecases.getClientByClientId(clientId);
            if (client == null)
                return null;

            if (!client.isEnabled()) {
                log.info("Client id disabled");
                throw new ClientPermissionException(String
                        .format("client with client_id %s is disabled", clientId));
            }

            return client;
        } catch (RuntimeException e) {
            throw e;
        } catch (Exception e) {
            log.error("Could not get client by clientId", e);
            return null;
        } finally {
            MDC.clear();
        }
    }
}
