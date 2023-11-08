/**
 *
 */
package com.onlyoffice.authorization.api.integration.services;

import com.onlyoffice.authorization.api.ContainerBase;
import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.external.mappers.ConsentMapper;
import com.onlyoffice.authorization.api.core.transfer.messages.ConsentMessage;
import com.onlyoffice.authorization.api.ports.repositories.ClientRepository;
import com.onlyoffice.authorization.api.ports.repositories.ConsentRepository;
import com.onlyoffice.authorization.api.ports.services.ConsentService;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.testcontainers.junit.jupiter.Testcontainers;

import java.sql.Timestamp;
import java.time.Instant;

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
    private ConsentService consentService;
    @Autowired
    private ConsentRepository consentRepository;
    @Autowired
    private ClientRepository clientRepository;

    @BeforeEach
    void beforeEach() {
        clientRepository.save(Client
                .builder()
                .clientId("mock")
                .build());
        consentService.saveConsent(ConsentMessage
                .builder()
                .principalName("mock")
                .registeredClientId("mock")
                .invalidated(false)
                .modifiedAt(Timestamp.from(Instant.now()))
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
        consentService.deleteConsent(ConsentMapper.INSTANCE.toMessage(c.get()));
        c = consentRepository.findById(new Consent.ConsentId("mock", "mock"));
        assertTrue(c.isEmpty());
    }
}
