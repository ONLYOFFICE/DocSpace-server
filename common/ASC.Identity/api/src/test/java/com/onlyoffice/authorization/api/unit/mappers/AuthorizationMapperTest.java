/**
 *
 */
package com.onlyoffice.authorization.api.unit.mappers;

import com.onlyoffice.authorization.api.core.entities.Authorization;
import com.onlyoffice.authorization.api.web.server.transfer.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.AuthorizationMapper;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.ActiveProfiles;

import java.sql.Timestamp;
import java.time.Instant;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotNull;

/**
 *
 */
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
                .accessTokenIssuedAt(Timestamp.from(Instant.now()))
                .authorizationCodeValue("mock")
                .authorizationCodeIssuedAt(Timestamp.from(Instant.now()))
                .authorizationCodeExpiresAt(Timestamp.from(Instant.now()))
                .authorizationCodeMetadata("mock")
                .accessTokenExpiresAt(Timestamp.from(Instant.now()))
                .accessTokenMetadata("mock")
                .accessTokenType("mock")
                .accessTokenScopes("mock")
                .refreshTokenValue("mock")
                .refreshTokenIssuedAt(Timestamp.from(Instant.now()))
                .refreshTokenExpiresAt(Timestamp.from(Instant.now()))
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
                .accessTokenIssuedAt(Timestamp.from(Instant.now()))
                .authorizationCodeValue("mock")
                .authorizationCodeIssuedAt(Timestamp.from(Instant.now()))
                .authorizationCodeExpiresAt(Timestamp.from(Instant.now()))
                .authorizationCodeMetadata("mock")
                .accessTokenExpiresAt(Timestamp.from(Instant.now()))
                .accessTokenMetadata("mock")
                .accessTokenType("mock")
                .accessTokenScopes("mock")
                .refreshTokenValue("mock")
                .refreshTokenIssuedAt(Timestamp.from(Instant.now()))
                .refreshTokenExpiresAt(Timestamp.from(Instant.now()))
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
