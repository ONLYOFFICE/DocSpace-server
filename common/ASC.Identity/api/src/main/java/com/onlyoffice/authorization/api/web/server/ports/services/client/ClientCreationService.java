package com.onlyoffice.authorization.api.web.server.ports.services.client;

import com.onlyoffice.authorization.api.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.exceptions.EntityCreationException;
import com.onlyoffice.authorization.api.core.usecases.repository.client.ClientPersistenceCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCreationUsecases;
import com.onlyoffice.authorization.api.web.client.transfer.PersonDTO;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import com.onlyoffice.authorization.api.web.security.crypto.Cipher;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import com.onlyoffice.authorization.api.web.server.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ClientMapper;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.ZonedDateTime;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.UUID;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class ClientCreationService implements ClientCreationUsecases {
    private final String DEFAULT_AUTHENTICATION = "client_secret_post";
    private final String PKCE_AUTHENTICATION = "none";

    private final RabbitMQConfiguration configuration;

    private final ClientPersistenceCreationUsecases creationUsecases;

    private final Cipher cipher;
    private final AmqpTemplate amqpClient;

    /**
     *
     * @param message
     * @return
     */
    @Transactional(
            timeout = 1250,
            rollbackFor = Exception.class
    )
    public ClientDTO saveClient(ClientMessage message) {
        log.info("Trying to create a new client");

        var secret = UUID.randomUUID().toString();
        message.setClientId(UUID.randomUUID().toString());
        try {
            message.setClientSecret(cipher.encrypt(secret));
        } catch (Exception e) {
            throw new UnsupportedOperationException(String
                    .format("Could not execute save client operation %s", e.getMessage()));
        }

        MDC.put("clientId", message.getClientId());
        log.info("Credentials have been generated for a new client");
        MDC.clear();

        var client = creationUsecases.saveClient(ClientMapper.INSTANCE
                .fromMessageToEntity(message));
        client.setClientSecret(secret);

        return ClientMapper.INSTANCE.fromEntityToQuery(client);
    }

    /**
     *
     * @param messages
     * @return
     */
    public List<String> saveClients(Iterable<ClientMessage> messages) {
        log.info("Trying to save new clients as a batch");

        var ids = new ArrayList<String>();
        for (ClientMessage message : messages) {
            try {
                MDC.put("clientId", message.getClientId());
                log.debug("Trying to save a new client", message);

                creationUsecases.saveClient(ClientMapper.INSTANCE
                        .fromMessageToEntity(message));

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

    /**
     *
     * @param clientDTO
     * @param tenant
     * @param person
     * @param tenantUrl
     * @return
     */
    public ClientDTO createClientAsync(
            CreateClientDTO clientDTO, TenantDTO tenant,
            PersonDTO person, String tenantUrl
    ) {
        MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
        MDC.put("tenantAlias", tenant.getTenantAlias());
        MDC.put("clientName", clientDTO.getName());
        log.info("Trying to create a new client creation task");

        try {
            var client = ClientMapper.INSTANCE.fromCommandToQuery(clientDTO);
            var secret = UUID.randomUUID().toString();
            var now = ZonedDateTime.now();
            var authenticationMethods = new HashSet<String>();
            authenticationMethods.add(DEFAULT_AUTHENTICATION);
            if (clientDTO.isAllowPkce())
                authenticationMethods.add(PKCE_AUTHENTICATION);

            client.setClientId(UUID.randomUUID().toString());
            client.setClientSecret(cipher.encrypt(secret));
            client.setTenant(tenant.getTenantId());
            client.setTenantUrl(tenantUrl);
            client.setCreatedOn(now);
            client.setModifiedOn(now);
            client.setCreatedBy(person.getEmail());
            client.setModifiedBy(person.getEmail());
            client.setAuthenticationMethods(authenticationMethods);

            var queue = configuration.getQueues().get("client");

            log.debug("Submitting a client creation task");

            amqpClient.convertAndSend(queue.getExchange(),
                    queue.getRouting(),
                    ClientMapper.INSTANCE.fromQueryToMessage(client));

            log.debug("Successfully submitted a client creation task");

            client.setClientSecret(secret);
            return client;
        } catch (Exception e) {
            throw new EntityCreationException(String
                    .format("could not create a new client creation task: %s", e.getMessage()));
        } finally {
            MDC.clear();
        }
    }
}
