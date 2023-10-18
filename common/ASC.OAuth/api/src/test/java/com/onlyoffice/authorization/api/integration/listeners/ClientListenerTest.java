/**
 *
 */
package com.onlyoffice.authorization.api.integration.listeners;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.core.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.external.listeners.ClientListener;
import com.onlyoffice.authorization.api.ports.repositories.ClientRepository;
import com.onlyoffice.authorization.api.ports.services.ClientService;
import lombok.SneakyThrows;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.testcontainers.junit.jupiter.Testcontainers;

import javax.sql.DataSource;
import java.util.Set;

import static org.junit.Assert.assertEquals;

/**
 *
 */
@Testcontainers
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
public class ClientListenerTest extends ContainerBase {
    @Autowired
    private DataSource dataSource;
    @Autowired
    private ClientListener clientListener;
    @Autowired
    private ClientService clientService;
    @Autowired
    private ClientRepository clientRepository;

    @Test
    @SneakyThrows
    void shouldCreateClientAsync() {
        clientService.clientAsyncCreationTask(CreateClientDTO
                .builder()
                .name("mock")
                .scopes(Set.of("mock"))
                .description("mock")
                .termsUrl("mock")
                .build(), 1);
        Thread.sleep(1000);
        assertEquals(1, clientListener.getLastBatchSize());
    }

    @Test
    @SneakyThrows
    void shouldCreateDeleteClientAsyncTask() {
        var c = clientService.clientAsyncCreationTask(CreateClientDTO
                .builder()
                .name("mock")
                .scopes(Set.of("mock"))
                .description("mock")
                .termsUrl("mock")
                .build(), 1);
        Thread.sleep(1000);
        assertEquals(1, clientListener.getLastBatchSize());
        clientService.clientAsyncDeletionTask(c.getClientId(), 1);
        Thread.sleep(1000);
        assertEquals(1, clientListener.getLastBatchSize());
    }
}
