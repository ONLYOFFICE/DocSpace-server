/**
 *
 */
package com.asc.authorization.web.security.oauth.repositories;

import com.asc.authorization.core.exceptions.EntityNotFoundException;
import com.asc.authorization.web.server.utilities.ClientMapper;
import com.asc.authorization.core.usecases.service.client.ClientRetrieveUsecases;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

/**
 *
 */
@Slf4j
@Repository
@RequiredArgsConstructor
@Transactional(readOnly = true, timeout = 2000)
public class AscRegisteredClientRepository implements RegisteredClientRepository {
    private final ClientRetrieveUsecases retrieveUsecases;

    private final ClientMapper clientMapper;

    /**
     *
     * @param registeredClient the {@link RegisteredClient}
     */
    public void save(RegisteredClient registeredClient) {
        MDC.put("clientId", registeredClient.getClientId());
        MDC.put("clientName", registeredClient.getClientName());
        log.error("ASC registered client repository supports only read operations");
        MDC.clear();
    }

    /**
     *
     * @param id the registration identifier
     * @return
     */
    public RegisteredClient findById(String id) {
        try {
            MDC.put("clientId", id);
            log.info("Trying to find registered client by id");

            var client = retrieveUsecases.getClientById(id);
            if (client == null)
                throw new EntityNotFoundException("Client with this clientId does not exist");
            return clientMapper.toRegisteredClient(client);
        } catch (Exception e) {
            log.warn("Could not find registered client", e);
            return null;
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param clientId the client identifier
     * @return
     */
    public RegisteredClient findByClientId(String clientId) {
        try {
            MDC.put("clientId", clientId);
            log.info("Trying to find registered client by id");

            var client = retrieveUsecases.getClientByClientId(clientId);
            if (client == null)
                throw new EntityNotFoundException("Client with this clientId does not exist");
            return clientMapper.toRegisteredClient(client);
        } catch (Exception e) {
            log.warn("Could not find registered client", e);
            return null;
        } finally {
            MDC.clear();
        }
    }
}
