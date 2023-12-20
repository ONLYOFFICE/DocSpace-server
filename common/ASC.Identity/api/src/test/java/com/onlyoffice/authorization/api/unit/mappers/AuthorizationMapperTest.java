/**
 *
 */
package com.onlyoffice.authorization.api.unit.mappers;

import com.onlyoffice.authorization.api.core.entities.Authorization;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.AuthorizationMapper;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.ActiveProfiles;

import java.time.ZonedDateTime;

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
                .modifiedAt(ZonedDateTime.now())
                .principalName("mock")
                .authorizationGrantType("mock")
                .authorizedScopes("mock")
                .invalidated(false)
                .attributes("mock")
                .accessTokenValue("mock")
                .accessTokenIssuedAt(ZonedDateTime.now())
                .authorizationCodeValue("mock")
                .authorizationCodeIssuedAt(ZonedDateTime.now())
                .authorizationCodeExpiresAt(ZonedDateTime.now())
                .authorizationCodeMetadata("mock")
                .accessTokenExpiresAt(ZonedDateTime.now())
                .accessTokenMetadata("mock")
                .accessTokenType("mock")
                .accessTokenScopes("mock")
                .refreshTokenValue("mock")
                .refreshTokenIssuedAt(ZonedDateTime.now())
                .refreshTokenExpiresAt(ZonedDateTime.now())
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
                .modifiedAt(ZonedDateTime.now())
                .principalName("mock")
                .authorizationGrantType("mock")
                .authorizedScopes("mock")
                .invalidated(false)
                .attributes("mock")
                .accessTokenValue("mock")
                .accessTokenIssuedAt(ZonedDateTime.now())
                .authorizationCodeValue("mock")
                .authorizationCodeIssuedAt(ZonedDateTime.now())
                .authorizationCodeExpiresAt(ZonedDateTime.now())
                .authorizationCodeMetadata("mock")
                .accessTokenExpiresAt(ZonedDateTime.now())
                .accessTokenMetadata("mock")
                .accessTokenType("mock")
                .accessTokenScopes("mock")
                .refreshTokenValue("mock")
                .refreshTokenIssuedAt(ZonedDateTime.now())
                .refreshTokenExpiresAt(ZonedDateTime.now())
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
