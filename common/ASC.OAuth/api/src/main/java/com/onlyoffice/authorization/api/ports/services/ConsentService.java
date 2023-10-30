/**
 *
 */
package com.onlyoffice.authorization.api.ports.services;

import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.core.transfer.response.ConsentDTO;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceRetrieveUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.api.external.mappers.ConsentMapper;
import com.onlyoffice.authorization.api.core.transfer.messages.ConsentMessage;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCreationUsecases;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.HashSet;
import java.util.Set;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;

/**
 *
 */
@Service
@RequiredArgsConstructor
@Slf4j
public class ConsentService implements ConsentRetrieveUsecases,
        ConsentCleanupUsecases, ConsentCreationUsecases {
    private final ConsentPersistenceRetrieveUsecases retrieveUsecases;
    private final ConsentPersistenceMutationUsecases mutationUsecases;

    public Set<ConsentDTO> getAllByPrincipalName(String principalName) throws RuntimeException {
        var response = new HashSet<ConsentDTO>();
        var results = retrieveUsecases.findAllByPrincipalName(principalName);
        results.forEach(r -> response.add(ConsentMapper.INSTANCE.toDTO(r)));
        return response;
    }

    @Transactional
    public void saveConsent(ConsentMessage consentMessage) {
        log.info("trying to save a new consent for {} and {}",
                consentMessage.getPrincipalName(), consentMessage.getRegisteredClientId());
        mutationUsecases.saveConsent(ConsentMapper.INSTANCE.toEntity(consentMessage));
    }

    @Transactional
    public void saveConsents(Iterable<ConsentMessage> consents) {
        log.info("trying to save consents");
        for (ConsentMessage consent : consents) {
            try {
                log.info("saving a new consent for {} and {}",
                        consent.getPrincipalName(), consent.getRegisteredClientId());
                mutationUsecases.saveConsent(ConsentMapper.INSTANCE.toEntity(consent));
            } catch (Exception e) {
                log.error(e.getMessage());
            }
        }
    }

    @Transactional
    public void deleteConsent(ConsentMessage consentMessage) {
        log.info("deleting a consent for {} and {}",
                consentMessage.getPrincipalName(), consentMessage.getRegisteredClientId());
        mutationUsecases.deleteById(new Consent.ConsentId(
                consentMessage.getRegisteredClientId(),
                consentMessage.getPrincipalName()));
    }

    @Transactional
    public void deleteConsents(Iterable<ConsentMessage> consents) {
        log.info("trying to delete all consents");
        mutationUsecases.deleteAll(StreamSupport
                .stream(consents.spliterator(), false)
                .map(c -> ConsentMapper.INSTANCE.toEntity(c))
                .collect(Collectors.toList()));
    }
}
