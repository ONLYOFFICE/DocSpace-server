/**
 *
 */
package com.onlyoffice.authorization.api.ports.services;

import com.onlyoffice.authorization.api.external.mappers.AuthorizationMapper;
import com.onlyoffice.authorization.api.core.transfer.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.core.usecases.repository.authorization.AuthorizationPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCreationUsecases;
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
@Service
@RequiredArgsConstructor
@Slf4j
public class AuthorizationService implements AuthorizationCleanupUsecases, AuthorizationCreationUsecases {
    private final AuthorizationPersistenceMutationUsecases mutationUsecases;

    @Transactional
    public void saveAuthorization(AuthorizationMessage authorizationMessage) {
        MDC.put("authorization_id", authorizationMessage.getId());
        log.info("Saving an authorization", authorizationMessage.getId());
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
                MDC.put("authorization_id", authorization.getId());
                log.info("Saving an authorization", authorization.getId());
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
        MDC.put("authorization_id", a.getId());
        log.info("Deleting an authorization", a.getId());
        MDC.clear();
        mutationUsecases.deleteById(a.getId());
    }

    @Transactional
    public void deleteAuthorizations(Iterable<AuthorizationMessage> authorizations) {
        log.info("Deleting authorizations");
        for (AuthorizationMessage authorization : authorizations) {
            try {
                MDC.put("authorization_id", authorization.getId());
                log.info("Deleting an authorization", authorization.getId());
                mutationUsecases.deleteById(authorization.getId());
            } catch (Exception e) {
                log.error("Could not remove an authorization", e);
            } finally {
                MDC.clear();
            }
        }
    }
}
