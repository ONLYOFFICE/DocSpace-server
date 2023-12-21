package com.onlyoffice.authorization.api.web.server.ports.services.authorization;

import com.onlyoffice.authorization.api.core.exceptions.EntityCleanupException;
import com.onlyoffice.authorization.api.core.usecases.repository.authorization.AuthorizationPersistenceCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCleanupUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class AuthorizationCleanupService implements AuthorizationCleanupUsecases {
    private final AuthorizationPersistenceCleanupUsecases cleanupUsecases;

    /**
     *
     * @param authorization
     */
    @Transactional(rollbackFor = Exception.class, timeout = 1250)
    public void deleteAuthorization(AuthorizationMessage authorization) {
        MDC.put("authorizationId", authorization.getId());
        log.info("Removing an authorization");
        MDC.clear();

        cleanupUsecases.deleteById(authorization.getId());
    }

    /**
     *
     * @param authorizations
     */
    @Transactional(rollbackFor = Exception.class, timeout = 3000)
    public void deleteAuthorizations(Iterable<AuthorizationMessage> authorizations) {
        log.info("Removing authorizations as a batch");

        for (AuthorizationMessage authorization : authorizations) {
            try {
                MDC.put("authorizationId", authorization.getId());
                log.debug("Deleting an authorization");

                cleanupUsecases.deleteById(authorization.getId());
            } catch (Exception e) {
                log.error("Could not remove an authorization", e);
            } finally {
                MDC.clear();
            }
        }
    }

    /**
     *
     * @param registeredClientId
     * @return
     */
    @Transactional(rollbackFor = Exception.class, timeout = 3000)
    public int deleteAuthorizationsByClientId(String registeredClientId) {
        MDC.put("clientId", registeredClientId);
        log.info("Removing authorizations as a batch by registeredClientId");

        try {
            var count = cleanupUsecases.deleteAllByClientId(registeredClientId);

            MDC.put("count", String.valueOf(count));
            log.debug("Successfully removed authorizations");

            return count;
        } catch (RuntimeException e) {
            throw new EntityCleanupException("Could not remove authorizations by client id", e);
        } finally {
            MDC.clear();
        }
    }
}
