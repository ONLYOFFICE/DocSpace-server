/**
 *
 */
package com.onlyoffice.authorization.api.ports.services;

import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.core.transfer.messages.ConsentMessage;
import com.onlyoffice.authorization.api.core.transfer.response.ConsentDTO;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceRetrieveUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.api.external.mappers.ConsentMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.sql.Timestamp;
import java.time.Instant;
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
    private final RabbitMQConfiguration configuration;

    private final ConsentPersistenceRetrieveUsecases retrieveUsecases;
    private final ConsentPersistenceMutationUsecases mutationUsecases;

    private final AmqpTemplate amqpTemplate;

    @Transactional(readOnly = true, rollbackFor = Exception.class, timeout = 2000)
    public Set<ConsentDTO> getAllByPrincipalName(String principalName) throws RuntimeException {
        MDC.put("principal_name", principalName);
        log.info("Trying to get all consents by principal name");
        MDC.clear();
        var response = new HashSet<ConsentDTO>();
        var results = retrieveUsecases.findAllByPrincipalName(principalName);
        results.forEach(r -> response.add(ConsentMapper.INSTANCE.toDTO(r)));
        return response;
    }

    @Transactional
    public void saveConsent(ConsentMessage consentMessage) {
        MDC.put("principal_name", consentMessage.getPrincipalName());
        MDC.put("client_id", consentMessage.getRegisteredClientId());
        log.info("Trying to save a new consent");
        MDC.clear();
        var entity = ConsentMapper.INSTANCE.toEntity(consentMessage);
        entity.setClient(Client.builder().clientId(consentMessage.getRegisteredClientId()).build());
        mutationUsecases.saveConsent(entity);
    }

    @Transactional
    public void saveConsents(Iterable<ConsentMessage> consents) {
        for (ConsentMessage consent : consents) {
            try {
                MDC.put("principal_name", consent.getPrincipalName());
                MDC.put("client_id", consent.getRegisteredClientId());
                log.info("Trying to save consent");
                var entity = ConsentMapper.INSTANCE.toEntity(consent);
                entity.setClient(Client.builder().clientId(consent.getRegisteredClientId()).build());
                log.info("Saving a new consent");
                mutationUsecases.saveConsent(entity);
            } catch (Exception e) {
                log.error("Could not persist a new consent", e);
            } finally {
                MDC.clear();
            }
        }
    }

    @Transactional
    public void deleteConsent(ConsentMessage consentMessage) {
        MDC.put("principal_name", consentMessage.getPrincipalName());
        MDC.put("client_id", consentMessage.getRegisteredClientId());
        log.info("Deleting a consent");
        MDC.clear();
        mutationUsecases.deleteById(new Consent.ConsentId(
                consentMessage.getRegisteredClientId(),
                consentMessage.getPrincipalName()));
    }

    @Transactional
    public void deleteConsents(Iterable<ConsentMessage> consents) {
        log.info("Trying to delete all consents");
        mutationUsecases.deleteAll(StreamSupport
                .stream(consents.spliterator(), false)
                .map(c -> ConsentMapper.INSTANCE.toEntity(c))
                .collect(Collectors.toList()));
    }

    public void asyncRevokeConsent(String clientId, String principalName) {
        this.amqpTemplate.convertAndSend(
                configuration.getConsent().getExchange(),
                configuration.getConsent().getRouting(),
                ConsentMessage
                        .builder()
                        .registeredClientId(clientId)
                        .principalName(principalName)
                        .scopes("***")
                        .modifiedAt(Timestamp.from(Instant.now()))
                        .invalidated(true)
                        .build()
        );
    }
}
