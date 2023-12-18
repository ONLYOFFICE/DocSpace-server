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

@Slf4j
@Service
@RequiredArgsConstructor
public class AuthorizationCleanupService implements AuthorizationCleanupUsecases {
    private final AuthorizationPersistenceCleanupUsecases cleanupUsecases;
    @Transactional(rollbackFor = Exception.class, timeout = 1250)
    public void deleteAuthorization(AuthorizationMessage authorization) {
        MDC.put("authorizationId", authorization.getId());
        log.info("Deleting an authorization");
        MDC.clear();
        cleanupUsecases.deleteById(authorization.getId());
    }

    @Transactional(rollbackFor = Exception.class, timeout = 2500)
    public void deleteAuthorizations(Iterable<AuthorizationMessage> authorizations) {
        log.info("Deleting authorizations");
        for (AuthorizationMessage authorization : authorizations) {
            try {
                MDC.put("authorizationId", authorization.getId());
                log.info("Deleting an authorization");
                cleanupUsecases.deleteById(authorization.getId());
            } catch (Exception e) {
                log.error("Could not remove an authorization", e);
            } finally {
                MDC.clear();
            }
        }
    }

    @Transactional(rollbackFor = Exception.class, timeout = 2500)
    public int deleteAuthorizationsByClientId(String registeredClientId) {
        MDC.put("clientId", registeredClientId);
        log.info("Deleting authorizations");
        try {
            var count = cleanupUsecases.deleteAllByClientId(registeredClientId);
            MDC.put("removed", String.valueOf(count));
            log.info("Successfully removed authorizations");
            return count;
        } catch (RuntimeException e) {
            throw new EntityCleanupException("Could not remove authorizations by client id", e);
        } finally {
            MDC.clear();
        }
    }
}
