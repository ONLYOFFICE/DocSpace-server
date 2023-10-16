/**
 *
 */
package com.onlyoffice.authorization.security.oauth.repositories;

import com.onlyoffice.authorization.core.exceptions.EntityNotFoundException;
import com.onlyoffice.authorization.core.exceptions.ReadOnlyOperationException;
import com.onlyoffice.authorization.core.usecases.service.client.ClientRetrieveUsecases;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.Cacheable;
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
    private final ClientRetrieveUsecases retrieveUsecases;

    private final CacheManager cacheManager;

    public void save(RegisteredClient registeredClient) {
        MDC.put("client_id", registeredClient.getClientId());
        MDC.put("client name", registeredClient.getClientName());
        log.info("request to save registered client");
        MDC.clear();
        throw new ReadOnlyOperationException("Docspace registered client repository supports only read operations");
    }

    @RateLimiter(name = "getRateLimiter", fallbackMethod = "findClientFallback")
    public RegisteredClient findById(String id) {
        MDC.put("client id", id);
        var cached = cacheManager.getCache("registered_clients").get(id);
        if (cached != null && (cached instanceof RegisteredClient client)) {
            log.info("found client in memory");
            MDC.clear();
            return client;
        }

        log.info("trying to find registered client");
        try {
            return retrieveUsecases.getClientById(id);
        } catch (EntityNotFoundException e) {
            log.error(e.getMessage());
            return null;
        } finally {
            MDC.clear();
        }
    }

    @Cacheable(value = "clients")
    @RateLimiter(name = "getRateLimiter", fallbackMethod = "findClientFallback")
    public RegisteredClient findByClientId(String clientId) {
        MDC.put("client_id", clientId);
        var cached = cacheManager.getCache("registered_clients").get(clientId);
        if (cached != null && (cached instanceof RegisteredClient client)) {
            log.info("found client in memory");
            MDC.clear();
            return client;
        }

        log.info("trying to find registered client");
        try {
            return retrieveUsecases.getClientByClientId(clientId);
        } catch (EntityNotFoundException e) {
            log.error(e.getMessage());
            return null;
        } finally {
            MDC.clear();
        }
    }

    private RegisteredClient findClientFallback(String id, Throwable e) {
        MDC.put("id/client_id", id);
        log.warn("registered client request is blocked due to rate-limiting {}", e.getMessage());
        MDC.clear();
        return null;
    }
}
