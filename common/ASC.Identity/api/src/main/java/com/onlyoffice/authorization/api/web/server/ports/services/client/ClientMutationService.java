package com.onlyoffice.authorization.api.web.server.ports.services.client;

import com.onlyoffice.authorization.api.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceRetrievalUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientMutationUsecases;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import com.onlyoffice.authorization.api.web.security.crypto.Cipher;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import com.onlyoffice.authorization.api.web.server.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.web.server.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.SecretDTO;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ClientMapper;
import jakarta.persistence.EntityNotFoundException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.data.util.Pair;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Isolation;
import org.springframework.transaction.annotation.Transactional;

import java.time.ZonedDateTime;
import java.util.HashSet;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class ClientMutationService implements ClientMutationUsecases {
    private final String DEFAULT_AUTHENTICATION = "client_secret_post";
    private final String PKCE_AUTHENTICATION = "none";

    private final RabbitMQConfiguration configuration;

    private final ClientPersistenceMutationUsecases mutationUsecases;
    private final ClientPersistenceRetrievalUsecases retrievalUsecases;

    private final CacheManager cacheManager;
    private final AmqpTemplate amqpClient;
    private final Cipher cipher;

    /**
     *
     * @param clientId
     * @param tenant
     * @return
     */
    @CacheEvict(cacheNames = {"clients"}, key = "#clientId")
    @Transactional(
            timeout = 1500,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public SecretDTO regenerateSecret(String clientId, TenantDTO tenant) {
        var secret = UUID.randomUUID().toString();

        MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
        MDC.put("tenantAlias", tenant.getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Regenerating client's secret");
        MDC.put("clientSecret", secret);
        log.debug("Generated a new client's secret");
        MDC.clear();

        try {
            mutationUsecases.regenerateClientSecretByClientId(clientId, tenant.getTenantId(),
                    cipher.encrypt(secret), ZonedDateTime.now());
        } catch (Exception e) {
            throw new UnsupportedOperationException(String
                    .format("Could not execute regenerate secret operation %s", e.getMessage()));
        }

        return SecretDTO.builder().clientSecret(secret).build();
    }

    /**
     *
     * @param activationDTO
     * @param clientId
     * @return
     */
    @CacheEvict(cacheNames = {"clients"}, key = "#clientId")
    @Transactional(
            timeout = 1500,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public boolean changeActivation(ChangeClientActivationDTO activationDTO, String clientId) {
        log.info("Changing client's activation", clientId, activationDTO.getStatus());

        try {
            mutationUsecases.changeActivation(clientId,
                    activationDTO.getStatus(), ZonedDateTime.now());
            return true;
        } catch (Exception e) {
            throw new UnsupportedOperationException(String
                    .format("Could not change client's activation %s", e.getMessage()));
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param clientDTO
     * @param clientId
     * @param tenant
     */
    @CacheEvict(cacheNames = {"clients"}, key = "#clientId")
    @Transactional(
            timeout = 1750,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public void updateClient(UpdateClientDTO clientDTO, String clientId, int tenant) {
        MDC.put("clientId", clientId);
        log.info("Trying to update a client");
        MDC.clear();

        var c = retrievalUsecases.findClientByClientIdAndTenant(clientId, tenant)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("Could not find client with client id %s for %d", clientId, tenant)));

        ClientMapper.INSTANCE.update(c, clientDTO);

        var authenticationMethods = "client_secret_post";
        if (clientDTO.isAllowPkce())
            authenticationMethods = String
                    .join(",", "client_secret_post", "none");

        c.setAuthenticationMethod(authenticationMethods);
        c.setModifiedBy(PersonContextContainer.context.get()
                .getResponse().getEmail());
    }

    /**
     *
     * @param updateClientPair
     * @return
     */
    @Transactional(
            timeout = 5000,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public Set<String> updateClients(Iterable<Pair<String, ClientMessage>> updateClientPair) {
        log.info("Trying to update clients as a batch");

        var ids = StreamSupport.stream(updateClientPair.spliterator(), false)
                .map(c -> c.getFirst())
                .collect(Collectors.toSet());

        var clients = retrievalUsecases.findClientsByClientIdIn(ids);
        clients.forEach(client -> {
            MDC.put("clientId", client.getClientId());
            log.debug("Trying to update a client");
            MDC.clear();

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

    /**
     *
     * @param updateClient
     * @param clientId
     */
    @CacheEvict(cacheNames = {"clients"}, key = "#clientId")
    public void updateClientAsync(UpdateClientDTO updateClient, String clientId) {
        MDC.put("clientId", clientId);
        log.info("Submitting a client update task", updateClient);

        try {
            var msg = ClientMapper.INSTANCE.fromCommandToMessage(updateClient);
            msg.setClientId(clientId);
            msg.setCommandCode(ClientMessage.ClientCommandCode.UPDATE_CLIENT);

            var authenticationMethods = new HashSet<String>();
            authenticationMethods.add(DEFAULT_AUTHENTICATION);
            if (msg.isAllowPkce())
                authenticationMethods.add(PKCE_AUTHENTICATION);
            msg.setAuthenticationMethod(String.join(",", authenticationMethods));

            var queue = configuration.getQueues().get("client");
            amqpClient.convertAndSend(queue.getExchange(), queue.getRouting(), msg);
        } catch (Exception e) {
            throw new UnsupportedOperationException(String
                    .format("Could not create a new client update task: %s", e.getMessage()));
        } finally {
            MDC.clear();
        }
    }
}
