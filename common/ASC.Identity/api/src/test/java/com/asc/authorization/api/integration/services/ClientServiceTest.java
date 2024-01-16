/**
 *
 */
package com.asc.authorization.api.integration.services;

import com.asc.authorization.api.ContainerBase;
import com.asc.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.asc.authorization.api.web.client.transfer.TenantDTO;
import com.asc.authorization.api.web.security.context.TenantContextContainer;
import com.asc.authorization.api.web.server.transfer.request.ChangeClientActivationDTO;
import com.asc.authorization.api.core.entities.Client;
import com.asc.authorization.api.web.security.crypto.Cipher;
import com.asc.authorization.api.web.server.ports.repositories.ClientRepository;
import com.asc.authorization.api.web.server.ports.services.client.ClientCleanupService;
import com.asc.authorization.api.web.server.ports.services.client.ClientCreationService;
import com.asc.authorization.api.web.server.ports.services.client.ClientMutationService;
import com.asc.authorization.api.web.server.ports.services.client.ClientRetrieveService;
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
    private ClientRetrieveService clientRetrieveService;
    @Autowired
    private ClientCleanupService clientCleanupService;
    @Autowired
    private ClientCreationService clientCreationService;
    @Autowired
    private ClientMutationService clientMutationService;
    @Autowired
    private ClientRepository clientRepository;
    @Autowired
    private Cipher cipher;

    @BeforeEach
    @SneakyThrows
    void beforeEach() {
        TenantContextContainer.context.set(APIClientDTOWrapper
                .<TenantDTO>builder()
                .status(200)
                .statusCode(200)
                .response(TenantDTO
                        .builder()
                        .name("mock")
                        .tenantAlias("mock")
                        .tenantId(1)
                        .build())
                .build());
        clientRepository.save(Client
                .builder()
                        .clientId("client")
                        .clientSecret(cipher.encrypt("secret"))
                        .tenant(1)
                        .invalidated(false)
                        .redirectUris("http://example.com")
                        .logoutRedirectUri("http://example.com")
                        .allowedOrigins("http://example.com")
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
        var c = clientRetrieveService.getClient("client");
        assertEquals("secret", c.getClientSecret());
    }

    @Test
    void shouldDeleteClient() {
        assertTrue(clientCleanupService.deleteClient( TenantDTO
                .builder()
                .tenantId(1)
                .name("mock")
                .tenantAlias("mock")
                .build(), "client"));
    }

    @Test
    void shouldChangeClientActivation() {
        clientMutationService.changeActivation(TenantDTO
                        .builder()
                        .tenantId(1)
                        .name("mock")
                        .tenantAlias("mock")
                        .build(),
                ChangeClientActivationDTO
                        .builder()
                        .status(false)
                        .build(),
                "client");

        var c = clientRetrieveService.getClient("client");
        assertFalse(c.isEnabled());
    }

    @Test
    void shouldRegenerateClientSecret() {
        var secret = clientRetrieveService.getClient("client").getClientSecret();
        var newSecret = clientMutationService.regenerateSecret(TenantDTO
                .builder()
                .tenantId(1)
                .name("mock")
                .tenantAlias("mock")
                .build(), "client");
        assertNotNull(newSecret.getClientSecret());
        assertNotEquals(newSecret.getClientSecret(), secret);
    }
}
