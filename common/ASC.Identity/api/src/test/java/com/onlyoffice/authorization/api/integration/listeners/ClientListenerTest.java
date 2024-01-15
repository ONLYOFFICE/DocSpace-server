/**
 *
 */
package com.onlyoffice.authorization.api.integration.listeners;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.PersonDTO;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
import com.onlyoffice.authorization.api.web.server.messaging.listeners.ClientListener;
import com.onlyoffice.authorization.api.web.server.ports.repositories.ClientRepository;
import com.onlyoffice.authorization.api.web.server.ports.services.client.ClientCleanupService;
import com.onlyoffice.authorization.api.web.server.ports.services.client.ClientCreationService;
import com.onlyoffice.authorization.api.web.server.ports.services.client.ClientRetrieveService;
import com.onlyoffice.authorization.api.web.server.transfer.request.CreateClientDTO;
import lombok.SneakyThrows;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.testcontainers.junit.jupiter.Testcontainers;

import javax.sql.DataSource;
import java.util.Set;
import java.util.UUID;

import static org.springframework.test.util.AssertionErrors.assertEquals;
import static org.springframework.test.util.AssertionErrors.assertNotNull;

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
    private ClientCreationService clientCreationService;
    @Autowired
    private ClientRetrieveService clientRetrieveService;
    @Autowired
    private ClientCleanupService clientCleanupService;
    @Autowired
    private ClientRepository clientRepository;

    @BeforeEach
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
        PersonContextContainer.context.set(APIClientDTOWrapper
                .<PersonDTO>builder()
                        .status(200)
                        .statusCode(200)
                        .response(PersonDTO
                                .builder()
                                .avatar("avatar")
                                .avatarSmall("smallAvatar")
                                .email("admin@admin.com")
                                .firstName("Admin")
                                .lastName("Admin")
                                .userName("Administrator")
                                .build())
                .build());
    }

    @Test
    @SneakyThrows
    void shouldCreateClientAsync() {
        var client = clientCreationService.createClientAsync(TenantDTO
                .builder()
                .tenantId(1)
                .name("mock")
                .tenantAlias("mock")
                .build(), CreateClientDTO
                .builder()
                .name("mock")
                .scopes(Set.of("mock"))
                .redirectUris(Set.of("http://example.com"))
                .allowedOrigins(Set.of("http://example.com"))
                .logoutRedirectUri("http://example.com")
                .description("mock")
                .termsUrl("mock")
                .build(), PersonDTO
                .builder()
                .id(UUID.randomUUID().toString())
                .build(), "http://127.0.0.1");
        Thread.sleep(2500);
        var c = clientRetrieveService.getClient(client.getClientId());
        assertNotNull("expected to get a non null client", c);
        assertEquals("expected to get matching client ids", client.getClientId(), c.getClientId());
    }

    @Test
    @SneakyThrows
    void shouldCreateDeleteClientAsyncTask() {
        var client = clientCreationService.createClientAsync(TenantDTO
                        .builder()
                        .tenantId(1)
                        .name("mock")
                        .tenantAlias("mock")
                        .build(),
                CreateClientDTO
                        .builder()
                        .name("mock")
                        .scopes(Set.of("mock"))
                        .redirectUris(Set.of("http://example.com"))
                        .allowedOrigins(Set.of("http://example.com"))
                        .logoutRedirectUri("http://example.com")
                        .description("mock")
                        .termsUrl("mock")
                        .build(),
                PersonDTO
                        .builder()
                        .id(UUID.randomUUID().toString())
                        .build(), "http://127.0.0.1");
        Thread.sleep(2500);
        var c = clientRetrieveService.getClient(client.getClientId());
        assertNotNull("expected to get a non null client", c);
        assertEquals("expected to get matching client ids", client.getClientId(), c.getClientId());

        clientCleanupService.deleteClientAsync(TenantDTO
                .builder()
                .tenantId(1)
                .name("mock")
                .tenantAlias("mock")
                .build(), c.getClientId());
    }
}
