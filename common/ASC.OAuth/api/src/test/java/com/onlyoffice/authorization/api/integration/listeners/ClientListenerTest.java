package com.onlyoffice.authorization.api.integration.listeners;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.dto.request.CreateClientDTO;
import com.onlyoffice.authorization.api.messaging.listeners.ClientListener;
import com.onlyoffice.authorization.api.repositories.ClientRepository;
import com.onlyoffice.authorization.api.services.ClientService;
import lombok.SneakyThrows;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.testcontainers.junit.jupiter.Testcontainers;

import javax.sql.DataSource;
import java.sql.Statement;
import java.util.Set;

import static org.junit.Assert.assertEquals;

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

    @BeforeEach
    @SneakyThrows
    void beforeEach() {
        Statement statement = dataSource.getConnection().createStatement();
        statement.executeUpdate("INSERT INTO tenants_tenants " + "VALUES (1, 'mock', 'mock')");
        statement.close();
    }

    @AfterEach
    @SneakyThrows
    void afterEach() {
        clientRepository.findAll().forEach(c -> clientRepository.delete(c));
        Statement statement = dataSource.getConnection().createStatement();
        statement.executeUpdate("DELETE FROM tenants_tenants t WHERE t.id = 1");
        statement.close();
    }

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
