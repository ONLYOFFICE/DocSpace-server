/**
 *
 */
package com.onlyoffice.authorization.api.integration.services;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.core.entities.Authorization;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.web.server.ports.repositories.AuthorizationRepository;
import com.onlyoffice.authorization.api.web.server.ports.services.authorization.AuthorizationCleanupService;
import com.onlyoffice.authorization.api.web.server.ports.services.authorization.AuthorizationCreationService;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.AuthorizationMapper;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.transaction.annotation.Transactional;
import org.testcontainers.junit.jupiter.Testcontainers;

import java.time.ZonedDateTime;

import static org.junit.Assert.assertNotNull;
import static org.junit.Assert.assertTrue;

/**
 *
 */
@Testcontainers
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
public class AuthorizationServiceTest extends ContainerBase {
    @Autowired
    private AuthorizationCreationService authorizationService;
    @Autowired
    private AuthorizationCleanupService cleanupService;
    @Autowired
    private AuthorizationRepository authorizationRepository;

    @BeforeEach
    void beforeEach() {
        authorizationService.saveAuthorization(AuthorizationMessage
                .builder()
                .registeredClientId("mock")
                .principalName("mock")
                .accessTokenExpiresAt(ZonedDateTime.now())
                .accessTokenIssuedAt(ZonedDateTime.now())
                .accessTokenMetadata("mock")
                .accessTokenScopes("mock")
                .accessTokenType("Bearer")
                .accessTokenValue("mock")
                .attributes("mock")
                .id("mock")
                .authorizationCodeExpiresAt(ZonedDateTime.now())
                .authorizationCodeIssuedAt(ZonedDateTime.now())
                .authorizationCodeValue("mock")
                .authorizationGrantType("mock")
                .build());
    }

    @AfterEach
    void afterEach() {
        authorizationRepository.deleteById("mock");
    }

    @Test
    @Transactional
    void shouldGetAuthorization() {
        var a = authorizationRepository.findById(new Authorization.AuthorizationId("mock", "mock"));
        assertNotNull(a);
    }

    @Test
    void shouldDeleteAuthorization() {
        var a = authorizationRepository.findById(new Authorization.AuthorizationId("mock", "mock"));
        cleanupService.deleteAuthorization(AuthorizationMapper.INSTANCE.toDTO(a.get()));
        a = authorizationRepository.findById(new Authorization.AuthorizationId("mock", "mock"));
        assertTrue(a.isEmpty());
    }
}
