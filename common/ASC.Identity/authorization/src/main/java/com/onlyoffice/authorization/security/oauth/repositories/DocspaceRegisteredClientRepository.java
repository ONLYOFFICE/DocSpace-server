/**
 *
 */
package com.onlyoffice.authorization.security.oauth.repositories;

import com.onlyoffice.authorization.core.exceptions.ReadOnlyOperationException;
import com.onlyoffice.authorization.core.usecases.service.client.ClientRetrieveUsecases;
import com.onlyoffice.authorization.security.crypto.aes.Cipher;
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
public class DocspaceRegisteredClientRepository implements RegisteredClientRepository {
    private final Cipher cipher;
    private final CacheManager cacheManager;

    private final ClientRetrieveUsecases retrieveUsecases;

    public void save(RegisteredClient registeredClient) {
        MDC.put("client_id", registeredClient.getClientId());
        MDC.put("client_name", registeredClient.getClientName());
        log.error("Docspace registered client repository supports only read operations");
        MDC.clear();
        throw new ReadOnlyOperationException("Docspace registered client repository supports only read operations");
    }

    public RegisteredClient findById(String id) {
        MDC.put("client_id", id);
        log.info("Trying to find registered client by id");
        var cache = cacheManager.getCache("registered_clients");
        var cached = cache.get(id);
        if (cached != null && (cached instanceof RegisteredClient client)) {
            log.info("Found registered client in memory");
            MDC.clear();
            return client;
        }

        try {
            var client = retrieveUsecases.getClientById(id);
            log.info("Found registered client in database. Decrypting the secret");
            client = RegisteredClient.from(client)
                    .clientSecret(cipher.decrypt(client.getClientSecret()))
                    .build();
            log.info("Putting registered client in cache");
            cache.put(id, client);
            return client;
        } catch (Exception e) {
            log.error("Could not find registered client", e);
            return null;
        } finally {
            MDC.clear();
        }
    }

    public RegisteredClient findByClientId(String clientId) {
        MDC.put("client_id", clientId);
        log.info("Trying to find registered client by id");
        var cache = cacheManager.getCache("registered_clients");
        var cached = cache.get(clientId);
        if (cached != null && (cached instanceof RegisteredClient client)) {
            log.info("Found registered client in memory");
            MDC.clear();
            return client;
        }

        try {
            var client = retrieveUsecases.getClientByClientId(clientId);
            log.info("Found registered client in database. Decrypting the secret");
            client = RegisteredClient.from(client)
                    .clientSecret(cipher.decrypt(client.getClientSecret()))
                    .build();
            log.info("Putting registered client in cache");
            cache.put(clientId, client);
            return client;
        } catch (Exception e) {
            log.error("Could not find registered client", e);
            return null;
        } finally {
            MDC.clear();
        }
    }

    private RegisteredClient findClientFallback(String id, Throwable e) {
        MDC.put("id/client_id", id);
        log.warn("Registered client request is blocked due to rate-limiting", e);
        MDC.clear();
        return null;
    }
}
