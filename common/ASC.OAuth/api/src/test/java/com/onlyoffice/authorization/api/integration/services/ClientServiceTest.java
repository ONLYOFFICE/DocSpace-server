package com.onlyoffice.authorization.api.integration.services;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.dto.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.entities.Client;
import com.onlyoffice.authorization.api.entities.Tenant;
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

import static org.junit.Assert.*;

@Testcontainers
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
public class ClientServiceTest extends ContainerBase {
    @Autowired
    private DataSource dataSource;
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
        clientRepository.save(Client
                .builder()
                        .clientId("client")
                        .clientSecret("secret")
                        .tenant(Tenant.builder().id(1).build())
                        .invalidated(false)
                        .enabled(true)
                        .scopes("accounts:read")
                        .authenticationMethod("mock")
                .build());
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
    void shouldGetClient() {
        var c = clientService.getClient("client", 1);
        assertEquals("secret", c.getClientSecret());
    }

    @Test
    void shouldDeleteClient() {
        assertTrue(clientService.deleteClient("client", 1));
    }

    @Test
    void shouldChangeClientActivation() {
        clientService.changeActivation(ChangeClientActivationDTO
                .builder().enabled(false).build(), "client");

        var c = clientService.getClient("client", 1);
        assertFalse(c.isEnabled());
    }

    @Test
    void shouldRegenerateClientSecret() {
        var secret = clientService.getClient("client", 1).getClientSecret();
        var newSecret = clientService.regenerateSecret("client", 1);
        assertNotNull(newSecret.getClientSecret());
        assertNotEquals(newSecret.getClientSecret(), secret);
    }
}
