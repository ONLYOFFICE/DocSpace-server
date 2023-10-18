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
        log.info("saving an authorization with id: {}", authorizationMessage.getId());
        mutationUsecases.saveAuthorization(AuthorizationMapper.INSTANCE.toEntity(authorizationMessage));
    }

    /**
     *
     * @param authorizations
     * @return a list of failed ids
     */
    @Transactional
    public List<String> saveAuthorizations(Iterable<AuthorizationMessage> authorizations) {
        log.info("saving authorizations");
        List<String> ids = new ArrayList<>();

        for (AuthorizationMessage authorization : authorizations) {
            try {
                log.info("Saving an authorization with id: {}", authorization.getId());
                mutationUsecases.saveAuthorization(AuthorizationMapper.INSTANCE.toEntity(authorization));
            } catch (Exception e) {
                ids.add(authorization.getId());
                log.error(e.getMessage());
            }
        }

        return ids;
    }

    @Transactional
    public void deleteAuthorization(AuthorizationMessage a) {
        log.info("deleting authorization with id: {}", a.getId());
        mutationUsecases.deleteById(a.getId());
    }

    @Transactional
    public void deleteAuthorizations(Iterable<AuthorizationMessage> authorizations) {
        log.info("deleting authorizations");
        for (AuthorizationMessage authorization : authorizations) {
            try {
                log.info("deleting authorization with id {}", authorization.getId());
                mutationUsecases.deleteById(authorization.getId());
            } catch (Exception e) {
                log.error(e.getMessage());
            }
        }
    }
}
