/**
 *
 */
package com.onlyoffice.authorization.api.integration.services;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.core.entities.Authorization;
import com.onlyoffice.authorization.api.external.mappers.AuthorizationMapper;
import com.onlyoffice.authorization.api.core.transfer.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.ports.repositories.AuthorizationRepository;
import com.onlyoffice.authorization.api.ports.services.AuthorizationService;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.transaction.annotation.Transactional;
import org.testcontainers.junit.jupiter.Testcontainers;

import java.time.Instant;
import java.util.Date;

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
    private AuthorizationService authorizationService;
    @Autowired
    private AuthorizationRepository authorizationRepository;

    @BeforeEach
    void beforeEach() {
        authorizationService.saveAuthorization(AuthorizationMessage
                .builder()
                .registeredClientId("mock")
                .principalName("mock")
                .accessTokenExpiresAt(Date.from(Instant.now()))
                .accessTokenIssuedAt(Date.from(Instant.now()))
                .accessTokenMetadata("mock")
                .accessTokenScopes("mock")
                .accessTokenType("Bearer")
                .accessTokenValue("mock")
                .attributes("mock")
                .id("mock")
                .authorizationCodeExpiresAt(Date.from(Instant.now()))
                .authorizationCodeIssuedAt(Date.from(Instant.now()))
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
        authorizationService.deleteAuthorization(AuthorizationMapper.INSTANCE.toDTO(a.get()));
        a = authorizationRepository.findById(new Authorization.AuthorizationId("mock", "mock"));
        assertTrue(a.isEmpty());
    }
}
