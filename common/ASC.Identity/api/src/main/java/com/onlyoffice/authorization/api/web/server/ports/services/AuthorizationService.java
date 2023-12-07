/**
 *
 */
package com.onlyoffice.authorization.api.web.server.ports.services;

import com.onlyoffice.authorization.api.core.usecases.repository.authorization.AuthorizationPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCreationUsecases;
import com.onlyoffice.authorization.api.web.server.transfer.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.AuthorizationMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class AuthorizationService implements AuthorizationCleanupUsecases, AuthorizationCreationUsecases {
    private final AuthorizationPersistenceMutationUsecases mutationUsecases;

    @Transactional
    public void saveAuthorization(AuthorizationMessage authorizationMessage) {
        MDC.put("authorizationId", authorizationMessage.getId());
        log.info("Saving an authorization");
        MDC.clear();
        mutationUsecases.saveAuthorization(AuthorizationMapper.INSTANCE.toEntity(authorizationMessage));
    }

    /**
     *
     * @param authorizations
     * @return a list of failed ids
     */
    @Transactional
    public List<String> saveAuthorizations(Iterable<AuthorizationMessage> authorizations) {
        log.info("Saving authorizations");
        List<String> ids = new ArrayList<>();

        for (AuthorizationMessage authorization : authorizations) {
            try {
                MDC.put("authorizationId", authorization.getId());
                log.info("Saving an authorization");
                mutationUsecases.saveAuthorization(AuthorizationMapper.INSTANCE.toEntity(authorization));
            } catch (Exception e) {
                ids.add(authorization.getId());
                log.error("Could not persist an authorization", e);
            } finally {
                MDC.clear();
            }
        }

        return ids;
    }

    @Transactional
    public void deleteAuthorization(AuthorizationMessage a) {
        MDC.put("authorizationId", a.getId());
        log.info("Deleting an authorization");
        MDC.clear();
        mutationUsecases.deleteById(a.getId());
    }

    @Transactional
    public void deleteAuthorizations(Iterable<AuthorizationMessage> authorizations) {
        log.info("Deleting authorizations");
        for (AuthorizationMessage authorization : authorizations) {
            try {
                MDC.put("authorizationId", authorization.getId());
                log.info("Deleting an authorization");
                mutationUsecases.deleteById(authorization.getId());
            } catch (Exception e) {
                log.error("Could not remove an authorization", e);
            } finally {
                MDC.clear();
            }
        }
    }
}
