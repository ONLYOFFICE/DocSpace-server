/**
 *
 */
package com.onlyoffice.authorization.api.integration.services;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.core.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.ports.repositories.ClientRepository;
import com.onlyoffice.authorization.api.ports.services.ClientService;
import lombok.SneakyThrows;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.testcontainers.junit.jupiter.Testcontainers;

import javax.sql.DataSource;

import static org.junit.Assert.*;

/**
 *
 */
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
        clientRepository.save(Client
                .builder()
                        .clientId("client")
                        .clientSecret("secret")
                        .tenant(1)
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
