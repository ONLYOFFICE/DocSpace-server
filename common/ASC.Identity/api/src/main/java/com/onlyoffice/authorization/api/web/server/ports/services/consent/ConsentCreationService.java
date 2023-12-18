package com.onlyoffice.authorization.api.web.server.ports.services.consent;

import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCreationUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ConsentMessage;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ConsentMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Slf4j
@Service
@RequiredArgsConstructor
public class ConsentCreationService implements ConsentCreationUsecases {
    private final ConsentPersistenceCreationUsecases mutationUsecases;

    @Transactional(rollbackFor = Exception.class, timeout = 1250)
    public void saveConsent(ConsentMessage consentMessage) {
        MDC.put("principalName", consentMessage.getPrincipalName());
        MDC.put("clientId", consentMessage.getRegisteredClientId());
        log.info("Trying to save a new consent");
        MDC.clear();
        var entity = ConsentMapper.INSTANCE.toEntity(consentMessage);
        entity.setClient(Client.builder().clientId(consentMessage.getRegisteredClientId()).build());
        mutationUsecases.saveConsent(entity);
    }

    @Transactional(timeout = 5000)
    public void saveConsents(Iterable<ConsentMessage> consents) {
        for (ConsentMessage consent : consents) {
            try {
                MDC.put("principalName", consent.getPrincipalName());
                MDC.put("clientId", consent.getRegisteredClientId());
                log.info("Trying to save consent");
                var entity = ConsentMapper.INSTANCE.toEntity(consent);
                entity.setClient(Client.builder()
                        .clientId(consent.getRegisteredClientId()).build());
                log.info("Saving a new consent");
                mutationUsecases.saveConsent(entity);
            } catch (Exception e) {
                log.error("Could not persist a new consent", e);
            } finally {
                MDC.clear();
            }
        }
    }
}
