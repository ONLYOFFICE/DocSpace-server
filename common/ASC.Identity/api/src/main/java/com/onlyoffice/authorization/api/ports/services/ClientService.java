/**
 *
 */
package com.onlyoffice.authorization.api.ports.services;

import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.exceptions.ClientCreationException;
import com.onlyoffice.authorization.api.core.exceptions.ClientDeletionException;
import com.onlyoffice.authorization.api.core.exceptions.ClientNotFoundException;
import com.onlyoffice.authorization.api.core.transfer.messages.ClientMessage;
import com.onlyoffice.authorization.api.core.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.core.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.PaginationDTO;
import com.onlyoffice.authorization.api.core.transfer.response.SecretDTO;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceRetrievalUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientRetrieveUsecases;
import com.onlyoffice.authorization.api.external.mappers.ClientMapper;
import com.onlyoffice.authorization.api.security.container.TenantContextContainer;
import com.onlyoffice.authorization.api.security.container.UserContextContainer;
import com.onlyoffice.authorization.api.security.crypto.Cipher;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.sql.Timestamp;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;

/**
 *
 */
@Service
@RequiredArgsConstructor
@Slf4j
public class ClientService implements ClientCleanupUsecases, ClientCreationUsecases,
        ClientMutationUsecases, ClientRetrieveUsecases {
    private final String SCOPE_OPENID = "openid";
    private final RabbitMQConfiguration configuration;

    private final ClientPersistenceMutationUsecases mutationUsecases;
    private final ClientPersistenceRetrievalUsecases retrievalUsecases;

    private final AmqpTemplate amqpTemplate;
    private final Cipher cipher;

    public void clientAsyncDeletionTask(String clientId, int tenant) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Trying to create a new client deletion task");
        try {
            this.amqpTemplate.convertAndSend(
                    configuration.getClient().getExchange(),
                    configuration.getClient().getRouting(),
                    ClientMessage
                            .builder()
                            .tenant(tenant)
                            .clientId(clientId)
                            .clientSecret(UUID.randomUUID().toString())
                            .scopes(Set.of("***"))
                            .redirectUris("***")
                            .modifiedBy(UserContextContainer.context.get()
                                    .getResponse().getUserName())
                            .enabled(false)
                            .invalidated(true)
                            .build());
        } catch (RuntimeException e) {
            log.error("Could not create a client deletion task", e);
            throw new ClientDeletionException(String
                    .format("could not create a client deletion task: %s", e.getMessage()));
        } finally {
            MDC.clear();
        }
    }

    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public boolean deleteClient(String id, int tenant) {
        MDC.put("tenantId", String.valueOf(tenant));
        MDC.put("clientId", id);
        log.info("Deleting a client");
        MDC.clear();
        if (mutationUsecases.deleteByClientIdAndTenant(id, tenant) < 1)
            throw new ClientNotFoundException(String
                    .format("could not find client with client id %s for %d", id, tenant));
        return true;
    }


    @SneakyThrows
    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public ClientDTO saveClient(ClientMessage message) {
        var context = TenantContextContainer.context.get();
        log.info("Trying to create a new client");
        message.setClientId(UUID.randomUUID().toString());
        message.setClientSecret(cipher.encrypt(UUID.randomUUID().toString()));
        message.getScopes().add(SCOPE_OPENID);
        MDC.put("clientId", message.getClientId());
        log.info("Credentials have been generated for a new client");
        MDC.clear();
        return ClientMapper.INSTANCE.fromEntityToQuery(mutationUsecases
                .saveClient(ClientMapper.INSTANCE.fromMessageToEntity(message)));
    }

    @Transactional
    public List<String> saveClients(Iterable<ClientMessage> messages) {
        log.info("Trying to save new clients");
        List<String> ids = new ArrayList<>();

        for (ClientMessage message : messages) {
            try {
                MDC.put("clientId", message.getClientId());
                message.getScopes().add(SCOPE_OPENID);
                log.debug("Trying to save a new client", message);
                mutationUsecases.saveClient(ClientMapper.INSTANCE.fromMessageToEntity(message));
                log.debug("Client has been saved", message);
            } catch (RuntimeException e) {
                ids.add(message.getClientId());
                log.error("Could not create a client", e);
            } finally {
                MDC.clear();
            }
        }

        return ids;
    }

    public ClientDTO clientAsyncCreationTask(CreateClientDTO clientDTO, int tenant, String tenantUrl) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientName", clientDTO.getName());
        log.info("Trying to create a new client creation task");
        try {
            ClientDTO client = ClientMapper.INSTANCE.fromCommandToQuery(clientDTO);
            var secret = UUID.randomUUID().toString();
            var now = Timestamp.from(Instant.now());
            var me = UserContextContainer.context.get()
                    .getResponse();

            client.setClientId(UUID.randomUUID().toString());
            client.setClientSecret(cipher.encrypt(secret));
            client.setTenant(tenant);
            client.setTenantUrl(tenantUrl);
            client.setCreatedOn(now);
            client.setModifiedOn(now);
            client.setCreatedBy(me.getEmail());
            client.setModifiedBy(me.getEmail());

            this.amqpTemplate.convertAndSend(configuration.getClient().getExchange(),
                    configuration.getClient().getRouting(),
                    ClientMapper.INSTANCE.fromQueryToMessage(client));

            client.setClientSecret(secret);
            client.getScopes().add(SCOPE_OPENID);
            return client;
        } catch (Exception e) {
            log.error("Could not create a new client creation task", e);
            throw new ClientCreationException(String
                    .format("could not create a new client creation task: %s", e.getMessage()));
        } finally {
            MDC.clear();
        }
    }

    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public ClientDTO updateClient(UpdateClientDTO clientDTO, String clientId, int tenant) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Trying to update a client");
        MDC.clear();
        var c = retrievalUsecases.findClientByClientIdAndTenant(clientId, tenant)
                .orElseThrow(() -> new ClientNotFoundException(String
                        .format("could not find client with client id %s for %d", clientId, tenant)));
        ClientMapper.INSTANCE.update(c, clientDTO);
        c.setModifiedBy(UserContextContainer.context.get()
                .getResponse().getEmail());
        return ClientMapper.INSTANCE.fromEntityToQuery(c);
    }

    @SneakyThrows
    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public SecretDTO regenerateSecret(String clientId, int tenant) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Regenerating client's secret");
        String secret = UUID.randomUUID().toString();
        MDC.put("clientSecret", secret);
        log.debug("Generated a new client's secret");
        MDC.clear();
        mutationUsecases.regenerateClientSecretByClientId(clientId,
                tenant, cipher.encrypt(secret));
        return SecretDTO.builder().clientSecret(secret).build();
    }

    @Transactional(rollbackFor = Exception.class, timeout = 2000)
    public boolean changeActivation(ChangeClientActivationDTO activationDTO, String clientId) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        MDC.put("status", String.valueOf(activationDTO.getStatus()));
        log.info("Changing client's activation", clientId, activationDTO.getStatus());
        try {
            mutationUsecases.changeActivation(clientId, activationDTO.getStatus());
            return true;
        } catch (RuntimeException e) {
            log.error("could not change client's activation", e);
            return false;
        } finally {
            MDC.clear();
        }
    }

    @Transactional(readOnly = true, rollbackFor = Exception.class, timeout = 2000)
    public ClientDTO getClient(String clientId) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Trying to get a client", clientId);
        MDC.clear();
        return retrievalUsecases
                .findById(clientId)
                .filter(c -> !c.isInvalidated())
                .map(c -> {
                    try {
                        var query = ClientMapper.INSTANCE.fromEntityToQuery(c);
                        query.setClientSecret(cipher.decrypt(query.getClientSecret()));
                        return query;
                    } catch (Exception e) {
                        MDC.put("tenantId", String.valueOf(context
                                .getResponse().getTenantId()));
                        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
                        MDC.put("clientId", clientId);
                        log.error("Could not map a client", e);
                        MDC.clear();
                        throw new ClientNotFoundException(String.
                                format("could not find and decrypt client secret: %s", e.getMessage()));
                    }
                })
                .orElseThrow(() -> new ClientNotFoundException(String
                        .format("could not find client with id %s", clientId)));
    }

    @Transactional(readOnly = true, rollbackFor = Exception.class, timeout = 2000)
    public PaginationDTO getTenantClients(int tenant, int page, int limit) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("page", String.valueOf(page));
        MDC.put("limit", String.valueOf(limit));
        log.info("Trying to get tenant clients", tenant, page, limit);
        MDC.clear();
        var data = retrievalUsecases
                .findAllByTenant(tenant, Pageable.ofSize(limit).withPage(page));

        var builder = PaginationDTO
                .<ClientDTO>builder()
                .page(page)
                .limit(limit)
                .data(data.stream()
                        .filter(c -> !c.isInvalidated())
                        .map(c -> ClientMapper.INSTANCE.fromEntityToQuery(c))
                        .collect(Collectors.toList()));

        if (data.hasPrevious())
            builder.previous(page - 1);

        if (data.hasNext())
            builder.next(page + 1);

        return builder.build();
    }
}
