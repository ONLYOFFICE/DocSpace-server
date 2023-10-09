package com.onlyoffice.authorization.api.services;

import com.onlyoffice.authorization.api.entities.Consent;
import com.onlyoffice.authorization.api.mappers.ConsentMapper;
import com.onlyoffice.authorization.api.messaging.messages.ConsentMessage;
import com.onlyoffice.authorization.api.usecases.repository.consent.ConsentPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.usecases.service.consent.ConsentCleanupUsecases;
import com.onlyoffice.authorization.api.usecases.service.consent.ConsentCreationUsecases;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.stream.Collectors;
import java.util.stream.StreamSupport;

@Service
@RequiredArgsConstructor
@Slf4j
public class ConsentService implements ConsentCleanupUsecases, ConsentCreationUsecases {
    private final ConsentPersistenceMutationUsecases mutationUsecases;

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
