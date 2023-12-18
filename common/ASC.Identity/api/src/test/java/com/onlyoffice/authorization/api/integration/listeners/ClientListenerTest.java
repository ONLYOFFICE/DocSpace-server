/**
 *
 */
package com.onlyoffice.authorization.api.integration.listeners;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.web.server.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.PersonDTO;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import com.onlyoffice.authorization.api.web.server.messaging.listeners.ClientListener;
import com.onlyoffice.authorization.api.web.server.ports.repositories.ClientRepository;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import lombok.SneakyThrows;
import org.junit.jupiter.api.BeforeEach;
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
        clientService.createClientAsync(CreateClientDTO
                .builder()
                .name("mock")
                .scopes(Set.of("mock"))
                .redirectUris(Set.of("http://example.com"))
                .allowedOrigins(Set.of("http://example.com"))
                .logoutRedirectUri("http://example.com")
                .description("mock")
                .termsUrl("mock")
                .build(), 1, "http://127.0.0.1");
        Thread.sleep(1000);
        assertEquals(1, clientListener.getLastBatchSize());
    }

    @Test
    @SneakyThrows
    void shouldCreateDeleteClientAsyncTask() {
        var c = clientService.createClientAsync(CreateClientDTO
                .builder()
                .name("mock")
                .scopes(Set.of("mock"))
                .redirectUris(Set.of("http://example.com"))
                .logoutRedirectUri("http://example.com")
                .allowedOrigins(Set.of("http://example.com"))
                .description("mock")
                .termsUrl("mock")
                .build(), 1, "http://127.0.0.1");
        Thread.sleep(1000);
        assertEquals(1, clientListener.getLastBatchSize());
        clientService.deleteClientAsync(c.getClientId(), 1);
        Thread.sleep(1000);
        assertEquals(1, clientListener.getLastBatchSize());
    }
}
