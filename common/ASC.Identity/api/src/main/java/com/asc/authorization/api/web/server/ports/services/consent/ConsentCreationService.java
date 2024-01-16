package com.asc.authorization.api.web.server.ports.services.consent;

import com.asc.authorization.api.core.entities.Client;
import com.asc.authorization.api.core.usecases.repository.consent.ConsentPersistenceCreationUsecases;
import com.asc.authorization.api.core.usecases.service.consent.ConsentCreationUsecases;
import com.asc.authorization.api.web.server.messaging.messages.ConsentMessage;
import com.asc.authorization.api.web.server.utilities.mappers.ConsentMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Isolation;
import org.springframework.transaction.annotation.Transactional;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class ConsentCreationService implements ConsentCreationUsecases {
    private final ConsentPersistenceCreationUsecases mutationUsecases;

    /**
     *
     * @param consentMessage
     */
    @Transactional(
            timeout = 1250,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public void saveConsent(ConsentMessage consentMessage) {
        MDC.put("principalName", consentMessage.getPrincipalName());
        MDC.put("clientId", consentMessage.getRegisteredClientId());
        log.info("Trying to save a new consent");
        MDC.clear();

        var entity = ConsentMapper.INSTANCE.toEntity(consentMessage);
        entity.setClient(Client.builder().clientId(consentMessage.getRegisteredClientId()).build());

        mutationUsecases.saveConsent(entity);
    }

    /**
     *
     * @param consents
     */
    @Transactional(
            timeout = 5000,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public void saveConsents(Iterable<ConsentMessage> consents) {
        log.info("Trying to save consents as a batch");
        for (ConsentMessage consent : consents) {
            try {
                MDC.put("principalName", consent.getPrincipalName());
                MDC.put("clientId", consent.getRegisteredClientId());
                log.debug("Trying to save a new consent");

                var entity = ConsentMapper.INSTANCE.toEntity(consent);
                entity.setClient(Client.builder()
                        .clientId(consent.getRegisteredClientId()).build());

                log.debug("Saving a new consent");

                mutationUsecases.saveConsent(entity);

                log.debug("Successfully saved a new consent");
            } catch (Exception e) {
                log.error("Could not persist a new consent", e);
            } finally {
                MDC.clear();
            }
        }
    }
}
