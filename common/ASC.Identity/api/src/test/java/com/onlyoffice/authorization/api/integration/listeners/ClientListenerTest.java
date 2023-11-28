/**
 *
 */
package com.onlyoffice.authorization.api.integration.listeners;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.core.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.DocspaceResponseDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.MeDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.TenantDTO;
import com.onlyoffice.authorization.api.external.listeners.ClientListener;
import com.onlyoffice.authorization.api.ports.repositories.ClientRepository;
import com.onlyoffice.authorization.api.ports.services.ClientService;
import com.onlyoffice.authorization.api.security.container.TenantContextContainer;
import com.onlyoffice.authorization.api.security.container.UserContextContainer;
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
        TenantContextContainer.context.set(DocspaceResponseDTO
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
        UserContextContainer.context.set(DocspaceResponseDTO
                .<MeDTO>builder()
                        .status(200)
                        .statusCode(200)
                        .response(MeDTO
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
        clientService.clientAsyncCreationTask(CreateClientDTO
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
        var c = clientService.clientAsyncCreationTask(CreateClientDTO
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
        clientService.clientAsyncDeletionTask(c.getClientId(), 1);
        Thread.sleep(1000);
        assertEquals(1, clientListener.getLastBatchSize());
    }
}
