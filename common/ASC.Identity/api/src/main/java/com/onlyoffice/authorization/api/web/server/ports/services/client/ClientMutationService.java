package com.onlyoffice.authorization.api.web.server.ports.services.client;

import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceRetrievalUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientMutationUsecases;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
import com.onlyoffice.authorization.api.web.security.crypto.Cipher;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import com.onlyoffice.authorization.api.web.server.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.web.server.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.SecretDTO;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ClientMapper;
import jakarta.persistence.EntityNotFoundException;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.data.util.Pair;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.sql.Timestamp;
import java.time.Instant;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;

@Slf4j
@Service
@RequiredArgsConstructor
public class ClientMutationService implements ClientMutationUsecases {
    private final RabbitMQConfiguration configuration;

    private final ClientPersistenceMutationUsecases mutationUsecases;
    private final ClientPersistenceRetrievalUsecases retrievalUsecases;

    private final CacheManager cacheManager;
    private final AmqpTemplate amqpClient;
    private final Cipher cipher;

    @SneakyThrows
    @CacheEvict(cacheNames = "clients", key = "#clientId")
    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public SecretDTO regenerateSecret(String clientId, int tenant) {
        var tenantContext = TenantContextContainer.context.get().getResponse();
        var secret = UUID.randomUUID().toString();

        MDC.put("tenantId", String.valueOf(tenantContext.getTenantId()));
        MDC.put("tenantAlias", tenantContext.getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Regenerating client's secret");
        MDC.put("clientSecret", secret);
        log.debug("Generated a new client's secret");
        MDC.clear();

        mutationUsecases.regenerateClientSecretByClientId(clientId,
                tenant, cipher.encrypt(secret), Timestamp.from(Instant.now()));

        return SecretDTO.builder().clientSecret(secret).build();
    }

    @CacheEvict(cacheNames = "clients", key = "#clientId")
    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public boolean changeActivation(ChangeClientActivationDTO activationDTO, String clientId) {
        var tenantContext = TenantContextContainer.context.get().getResponse();

        MDC.put("tenantId", String.valueOf(tenantContext.getTenantId()));
        MDC.put("tenantAlias", tenantContext.getTenantAlias());
        MDC.put("clientId", clientId);
        MDC.put("status", String.valueOf(activationDTO.getStatus()));
        log.info("Changing client's activation", clientId, activationDTO.getStatus());

        try {
            mutationUsecases.changeActivation(clientId,
                    activationDTO.getStatus(), Timestamp.from(Instant.now()));
            return true;
        } catch (RuntimeException e) {
            log.error("could not change client's activation", e.getMessage());
            return false;
        } finally {
            MDC.clear();
        }
    }

    @CacheEvict(cacheNames = "clients", key = "#clientId")
    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public void updateClient(UpdateClientDTO clientDTO, String clientId, int tenant) {
        var tenantContext = TenantContextContainer.context.get().getResponse();

        MDC.put("tenantId", String.valueOf(tenantContext.getTenantId()));
        MDC.put("tenantAlias", tenantContext.getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Trying to update a client");
        MDC.clear();

        var c = retrievalUsecases.findClientByClientIdAndTenant(clientId, tenant)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find client with client id %s for %d", clientId, tenant)));

        ClientMapper.INSTANCE.update(c, clientDTO);

        var authenticationMethods = "client_secret_post";
        if (clientDTO.isAllowPkce())
            authenticationMethods = String
                    .join(",", "client_secret_post", "none");

        c.setAuthenticationMethod(authenticationMethods);
        c.setModifiedBy(PersonContextContainer.context.get()
                .getResponse().getEmail());
    }

    public Set<String> updateClients(Iterable<Pair<String, ClientMessage>> updateClientPair) {
        var ids = StreamSupport.stream(updateClientPair.spliterator(), false)
                .map(c -> c.getFirst())
                .collect(Collectors.toSet());

        var clients = retrievalUsecases.findClientsByClientIdIn(ids);
        clients.forEach(client -> {
            var cl = StreamSupport.stream(updateClientPair.spliterator(), false)
                    .filter(c -> c.getFirst().equalsIgnoreCase(client.getClientId()))
                    .findFirst();
            if (cl == null || cl.isEmpty())
                return;
            ClientMapper.INSTANCE.update(client, ClientMapper.INSTANCE
                    .fromMessageToEntity(cl.get().getSecond()));
            cacheManager.getCache("clients")
                    .evictIfPresent(client.getClientId());
        });

        return ids;
    }

    @CacheEvict(cacheNames = "clients", key = "#clientId")
    public void updateClientAsync(UpdateClientDTO updateClient, String clientId) {
        try {
            var msg = ClientMapper.INSTANCE.fromCommandToMessage(updateClient);
            msg.setClientId(clientId);
            msg.setCommandCode(ClientMessage.ClientCommandCode.UPDATE_CLIENT);

            amqpClient.convertAndSend(configuration.getClient().getExchange(),
                    configuration.getClient().getRouting(), msg);
        } catch (Exception e) {
            log.error("Could not create a new client update task", e);
            throw new EntityNotFoundException(String
                    .format("Could not create a new client update task: %s", e.getMessage()));
        } finally {
            MDC.clear();
        }
    }
}
