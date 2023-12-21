package com.onlyoffice.authorization.api.web.server.ports.services.client;

import com.onlyoffice.authorization.api.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.exceptions.EntityCleanupException;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCleanupUsecases;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Set;
import java.util.UUID;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class ClientCleanupService implements ClientCleanupUsecases {
    private final RabbitMQConfiguration configuration;
    private final AmqpTemplate amqpClient;

    private final ClientPersistenceCleanupUsecases cleanupUsecases;

    /**
     *
     * @param clientId
     */
    @CacheEvict(cacheNames = "clients", key = "#clientId")
    public void deleteClientAsync(String clientId) {
        log.info("Trying to create a new client deletion task");

        try {
            var queue = configuration.getQueues().get("client");
            amqpClient.convertAndSend(
                    queue.getExchange(),
                    queue.getRouting(),
                    ClientMessage
                            .builder()
                            .tenant(0)
                            .clientId(clientId)
                            .clientSecret(UUID.randomUUID().toString())
                            .scopes(Set.of("***"))
                            .redirectUris("***")
                            .modifiedBy(PersonContextContainer.context.get()
                                    .getResponse().getUserName())
                            .enabled(false)
                            .invalidated(true)
                            .build());
        } catch (RuntimeException e) {
            throw new EntityCleanupException(String
                    .format("Could not create a client deletion task: %s", e.getMessage()));
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param id
     * @param tenant
     * @return
     */
    @CacheEvict(cacheNames = "clients", key = "#id")
    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public boolean deleteClient(String id, int tenant) {
        MDC.put("tenantId", String.valueOf(tenant));
        MDC.put("clientId", id);
        log.info("Deleting a client");
        MDC.clear();

        if (cleanupUsecases.deleteByClientIdAndTenant(id, tenant) < 1)
            throw new EntityCleanupException(String
                    .format("could not delete client with client id %s for %d", id, tenant));

        return true;
    }
}
