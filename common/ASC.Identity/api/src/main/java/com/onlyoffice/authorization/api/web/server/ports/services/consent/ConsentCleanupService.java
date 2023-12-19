package com.onlyoffice.authorization.api.web.server.ports.services.consent;

import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCleanupUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ConsentMessage;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ConsentMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.ZonedDateTime;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;

@Slf4j
@Service
@RequiredArgsConstructor
public class ConsentCleanupService implements ConsentCleanupUsecases {
    private final RabbitMQConfiguration configuration;
    private final AmqpTemplate amqpTemplate;

    private final ConsentPersistenceCleanupUsecases cleanupUsecases;

    @Transactional(rollbackFor = Exception.class, timeout = 1500)
    public void deleteConsent(ConsentMessage consentMessage) {
        MDC.put("principalName", consentMessage.getPrincipalName());
        MDC.put("clientId", consentMessage.getRegisteredClientId());
        log.info("Deleting a consent");
        MDC.clear();

        cleanupUsecases.deleteById(new Consent.ConsentId(
                consentMessage.getRegisteredClientId(),
                consentMessage.getPrincipalName()));
    }

    @Transactional(rollbackFor = Exception.class, timeout = 2500)
    public void deleteConsents(Iterable<ConsentMessage> consents) {
        log.info("Trying to delete all consents");
        cleanupUsecases.deleteAll(StreamSupport
                .stream(consents.spliterator(), false)
                .map(c -> ConsentMapper.INSTANCE.toEntity(c))
                .collect(Collectors.toList()));
    }

    public void asyncRevokeConsent(String clientId, String principalName) {
        amqpTemplate.convertAndSend(
                configuration.getConsent().getExchange(),
                configuration.getConsent().getRouting(),
                ConsentMessage
                        .builder()
                        .registeredClientId(clientId)
                        .principalName(principalName)
                        .scopes("***")
                        .modifiedAt(ZonedDateTime.now())
                        .invalidated(true)
                        .build()
        );
    }
}
