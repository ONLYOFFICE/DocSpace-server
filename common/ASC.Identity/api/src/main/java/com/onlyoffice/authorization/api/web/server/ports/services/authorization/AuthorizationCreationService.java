/**
 *
 */
package com.onlyoffice.authorization.api.web.server.ports.services.authorization;

import com.onlyoffice.authorization.api.core.usecases.repository.authorization.AuthorizationPersistenceCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCreationUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.AuthorizationMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Isolation;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class AuthorizationCreationService implements AuthorizationCreationUsecases {
    private final AuthorizationPersistenceCreationUsecases creationUsecases;

    /**
     *
     * @param authorizationMessage
     */
    @Transactional(
            timeout = 1500,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public void saveAuthorization(AuthorizationMessage authorizationMessage) {
        MDC.put("authorizationId", authorizationMessage.getId());
        log.info("Persisting an authorization");
        MDC.clear();

        creationUsecases.saveAuthorization(AuthorizationMapper.INSTANCE
                .toEntity(authorizationMessage));
    }

    /**
     *
     * @param authorizations
     * @return a list of failed ids
     */
    @Transactional(
            timeout = 5000,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public List<String> saveAuthorizations(Iterable<AuthorizationMessage> authorizations) {
        log.info("Persisting authorizations as a batch");

        var ids = new ArrayList<String>();
        for (AuthorizationMessage authorization : authorizations) {
            try {
                MDC.put("authorizationId", authorization.getId());
                log.debug("Saving an authorization");

                creationUsecases.saveAuthorization(AuthorizationMapper.INSTANCE.toEntity(authorization));
            } catch (Exception e) {
                ids.add(authorization.getId());
                log.error("Could not persist an authorization", e);
            } finally {
                MDC.clear();
            }
        }

        return ids;
    }
}
