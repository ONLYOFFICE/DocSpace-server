/**
 *
 */
package com.onlyoffice.authorization.web.security.oauth.repositories;

import com.onlyoffice.authorization.core.exceptions.EntityNotFoundException;
import com.onlyoffice.authorization.core.usecases.service.client.ClientRetrieveUsecases;
import com.onlyoffice.authorization.web.security.crypto.cipher.Cipher;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.cache.CacheManager;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

/**
 *
 */
@Slf4j
@Repository
@RequiredArgsConstructor
@Transactional(readOnly = true, timeout = 2000)
public class AscRegisteredClientRepository implements RegisteredClientRepository {
    private final String CACHE_NAME = "registered_clients";

    private final Cipher cipher;
    private final CacheManager cacheManager;

    private final ClientRetrieveUsecases retrieveUsecases;

    /**
     *
     * @param registeredClient the {@link RegisteredClient}
     */
    public void save(RegisteredClient registeredClient) {
        MDC.put("clientId", registeredClient.getClientId());
        MDC.put("clientName", registeredClient.getClientName());
        log.error("Docspace registered client repository supports only read operations");
        MDC.clear();
    }

    /**
     *
     * @param id the registration identifier
     * @return
     */
    public RegisteredClient findById(String id) {
        MDC.put("clientId", id);
        log.info("Trying to find registered client by id");

        var cache = cacheManager.getCache(CACHE_NAME);
        var cached = cache.get(id);
        if (cached != null && (cached instanceof RegisteredClient client)) {
            log.info("Found registered client in memory");
            MDC.clear();

            return client;
        }

        try {
            var client = retrieveUsecases.getClientById(id);
            if (client == null)
                throw new EntityNotFoundException("Client with this clientId does not exist");

            log.info("Found registered client in database. Decrypting the secret");

            client = RegisteredClient.from(client)
                    .clientSecret(cipher.decrypt(client.getClientSecret()))
                    .build();

            log.info("Putting registered client in cache");

            cache.put(id, client);
            return client;
        } catch (Exception e) {
            log.warn("Could not find registered client", e);
            return null;
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param clientId the client identifier
     * @return
     */
    public RegisteredClient findByClientId(String clientId) {
        MDC.put("clientId", clientId);
        log.info("Trying to find registered client by id");

        var cache = cacheManager.getCache(CACHE_NAME);
        var cached = cache.get(clientId);
        if (cached != null && (cached instanceof RegisteredClient client)) {
            log.info("Found registered client in memory");
            MDC.clear();

            return client;
        }

        try {
            var client = retrieveUsecases.getClientByClientId(clientId);
            if (client == null)
                throw new EntityNotFoundException("Client with this clientId does not exist");

            log.info("Found registered client in database. Decrypting the secret");

            client = RegisteredClient.from(client)
                    .clientSecret(cipher.decrypt(client.getClientSecret()))
                    .build();

            log.info("Putting registered client in cache");

            cache.put(clientId, client);
            return client;
        } catch (Exception e) {
            log.warn("Could not find registered client", e);
            return null;
        } finally {
            MDC.clear();
        }
    }
}
