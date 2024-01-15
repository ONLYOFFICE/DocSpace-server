package com.onlyoffice.authorization.api.web.server.ports.services.client;

import com.onlyoffice.authorization.api.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.exceptions.EntityCleanupException;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCleanupUsecases;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
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
     * @param tenant
     * @param clientId
     */
    @CacheEvict(cacheNames = {"identityClients"}, key = "#clientId")
    public void deleteClientAsync(TenantDTO tenant, String clientId) {
        MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
        MDC.put("tenantAlias", tenant.getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Trying to create a new client deletion task");

        try {
            var queue = configuration.getQueues().get("client");
            amqpClient.convertAndSend(
                    queue.getExchange(),
                    queue.getRouting(),
                    ClientMessage
                            .builder()
                            .tenant(tenant.getTenantId())
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
     * @param tenant
     * @param id
     * @return
     */
    @CacheEvict(cacheNames = {"identityClients"}, key = "#id")
    @Transactional(
            timeout = 2000,
            rollbackFor = Exception.class
    )
    public boolean deleteClient(TenantDTO tenant, String id) {
        MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
        MDC.put("tenantAlias", tenant.getTenantAlias());
        MDC.put("clientId", id);
        log.info("Deleting a client");
        MDC.clear();

        if (cleanupUsecases.deleteByTenantAndClientId(tenant.getTenantId(), id) < 1)
            throw new EntityCleanupException(String
                    .format("could not delete client with client id %s for %d", id, tenant));

        return true;
    }
}
