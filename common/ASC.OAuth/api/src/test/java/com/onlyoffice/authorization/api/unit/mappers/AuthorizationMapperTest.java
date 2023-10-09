package com.onlyoffice.authorization.api.unit.mappers;

import com.onlyoffice.authorization.api.entities.Authorization;
import com.onlyoffice.authorization.api.mappers.AuthorizationMapper;
import com.onlyoffice.authorization.api.messaging.messages.AuthorizationMessage;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.ActiveProfiles;

import java.sql.Date;
import java.sql.Timestamp;
import java.time.Instant;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotNull;

@ActiveProfiles("test")
public class AuthorizationMapperTest {
    @Test
    void shouldMapEntityToMessage() {
        var a = Authorization.builder()
                .id("mock")
                .state("mock")
                .modifiedAt(Timestamp.from(Instant.now()))
                .principalName("mock")
                .authorizationGrantType("mock")
                .authorizedScopes("mock")
                .invalidated(false)
                .attributes("mock")
                .accessTokenValue("mock")
                .accessTokenIssuedAt(Date.from(Instant.now()))
                .authorizationCodeValue("mock")
                .authorizationCodeIssuedAt(Date.from(Instant.now()))
                .authorizationCodeExpiresAt(Date.from(Instant.now()))
                .authorizationCodeMetadata("mock")
                .accessTokenExpiresAt(java.util.Date.from(Instant.now()))
                .accessTokenMetadata("mock")
                .accessTokenType("mock")
                .accessTokenScopes("mock")
                .refreshTokenValue("mock")
                .refreshTokenIssuedAt(Date.from(Instant.now()))
                .refreshTokenExpiresAt(Date.from(Instant.now()))
                .refreshTokenMetadata("mock")
                .registeredClientId("mock")
                .refreshTokenMetadata("mock")
                .build();
        var m = AuthorizationMapper.INSTANCE.toDTO(a);
        assertNotNull(m);
        var na = AuthorizationMapper.INSTANCE.toEntity(m);
        assertEquals(a, na);
    }

    @Test
    void shouldMapMessageToEntity() {
        var m = AuthorizationMessage.builder()
                .id("mock")
                .state("mock")
                .modifiedAt(Timestamp.from(Instant.now()))
                .principalName("mock")
                .authorizationGrantType("mock")
                .authorizedScopes("mock")
                .invalidated(false)
                .attributes("mock")
                .accessTokenValue("mock")
                .accessTokenIssuedAt(Date.from(Instant.now()))
                .authorizationCodeValue("mock")
                .authorizationCodeIssuedAt(Date.from(Instant.now()))
                .authorizationCodeExpiresAt(Date.from(Instant.now()))
                .authorizationCodeMetadata("mock")
                .accessTokenExpiresAt(java.util.Date.from(Instant.now()))
                .accessTokenMetadata("mock")
                .accessTokenType("mock")
                .accessTokenScopes("mock")
                .refreshTokenValue("mock")
                .refreshTokenIssuedAt(Date.from(Instant.now()))
                .refreshTokenExpiresAt(Date.from(Instant.now()))
                .refreshTokenMetadata("mock")
                .registeredClientId("mock")
                .refreshTokenMetadata("mock")
                .build();
        var a = AuthorizationMapper.INSTANCE.toEntity(m);
        assertNotNull(a);
        var nm = AuthorizationMapper.INSTANCE.toDTO(a);
        assertEquals(m, nm);
    }
}
