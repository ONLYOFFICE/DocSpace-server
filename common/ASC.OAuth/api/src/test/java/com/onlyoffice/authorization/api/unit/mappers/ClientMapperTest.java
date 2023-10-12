package com.onlyoffice.authorization.api.unit.mappers;

import com.onlyoffice.authorization.api.entities.Client;
import com.onlyoffice.authorization.api.mappers.ClientMapper;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.ActiveProfiles;

import java.sql.Timestamp;
import java.time.Instant;

import static org.junit.Assert.assertNotNull;

@ActiveProfiles("test")
public class ClientMapperTest {
    private Timestamp iat = Timestamp.from(Instant.now());
    private Client client = Client
            .builder()
            .clientId("client")
            .name("name")
            .description("description")
            .clientSecret("secret")
            .termsUrl("terms")
            .policyUrl("policy")
            .logoUrl("logo")
            .clientIssuedAt(iat)
            .authenticationMethod("mock")
            .redirectUri("redirect")
            .logoutRedirectUri("logoutredirect")
            .scopes("scopes")
            .tenant(1)
            .enabled(true)
            .invalidated(false)
            .build();

    @Test
    void shouldMapClientToQuery() {
        var q = ClientMapper.INSTANCE.fromEntityToQuery(client);
        assertNotNull(q);
    }

    @Test
    void shouldMapClientToMessage() {
        var q = ClientMapper.INSTANCE.fromEntityToQuery(client);
        assertNotNull(ClientMapper.INSTANCE.fromQueryToMessage(q));
    }
}
