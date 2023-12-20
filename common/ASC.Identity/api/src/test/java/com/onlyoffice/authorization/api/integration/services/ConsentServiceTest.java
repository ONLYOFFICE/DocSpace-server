/**
 *
 */
package com.onlyoffice.authorization.api.integration.services;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.core.entities.Authorization;
import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ConsentMessage;
import com.onlyoffice.authorization.api.web.server.ports.repositories.AuthorizationRepository;
import com.onlyoffice.authorization.api.web.server.ports.repositories.ClientRepository;
import com.onlyoffice.authorization.api.web.server.ports.repositories.ConsentRepository;
import com.onlyoffice.authorization.api.web.server.ports.services.consent.ConsentCleanupService;
import com.onlyoffice.authorization.api.web.server.ports.services.consent.ConsentCreationService;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ConsentMapper;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
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
public class ConsentServiceTest extends ContainerBase {
    @Autowired
    private ConsentCreationService consentCreationService;
    @Autowired
    private ConsentCleanupService consentCleanupService;
    @Autowired
    private ConsentRepository consentRepository;
    @Autowired
    private ClientRepository clientRepository;
    @Autowired
    private AuthorizationRepository authorizationRepository;

    @BeforeEach
    void beforeEach() {
        clientRepository.save(Client
                .builder()
                .clientId("mock")
                .build());
        authorizationRepository.saveAuthorization(Authorization
                .builder()
                .id("mock")
                .principalName("mock")
                .registeredClientId("mock")
                .build());
        consentCreationService.saveConsent(ConsentMessage
                .builder()
                .principalName("mock")
                .registeredClientId("mock")
                .invalidated(false)
                .modifiedAt(ZonedDateTime.now())
                .scopes("mock")
                .build());
    }

    @AfterEach
    void afterEach() {
        consentRepository.deleteById(new Consent.ConsentId("mock", "mock"));
    }

    @Test
    void shouldGetConsent() {
        var a = consentRepository.findById(new Consent.ConsentId("mock", "mock"));
        assertNotNull(a);
    }

    @Test
    void shouldDeleteConsent() {
        var c = consentRepository
                .findById(new Consent.ConsentId("mock", "mock"));
        consentCleanupService.deleteConsent(ConsentMapper.INSTANCE.toMessage(c.get()));
        c = consentRepository.findById(new Consent.ConsentId("mock", "mock"));
        assertTrue(c.isEmpty());
    }
}
