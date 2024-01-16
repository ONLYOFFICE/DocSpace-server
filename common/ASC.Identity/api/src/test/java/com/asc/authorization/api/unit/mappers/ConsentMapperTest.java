/**
 *
 */
package com.asc.authorization.api.unit.mappers;

import com.asc.authorization.api.core.entities.Consent;
import com.asc.authorization.api.web.server.messaging.messages.ConsentMessage;
import com.asc.authorization.api.web.server.utilities.mappers.ConsentMapper;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.ActiveProfiles;

import java.time.ZonedDateTime;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotNull;

/**
 *
 */
@ActiveProfiles("test")
public class ConsentMapperTest {
    @Test
    void shouldMapEntityToMessage() {
        var c = Consent.builder()
                .principalName("mock")
                .registeredClientId("mock")
                .invalidated(false)
                .modifiedAt(ZonedDateTime.now())
                .scopes("mock")
                .build();
        var m = ConsentMapper.INSTANCE.toMessage(c);
        assertNotNull(m);
        var nc = ConsentMapper.INSTANCE.toEntity(m);
        assertEquals(c, nc);
    }

    @Test
    void shouldMapMessageToEntity() {
        var m = ConsentMessage.builder()
                .scopes("mock")
                .registeredClientId("mock")
                .principalName("mock")
                .modifiedAt(ZonedDateTime.now())
                .invalidated(false)
                .build();
        var c = ConsentMapper.INSTANCE.toEntity(m);
        assertNotNull(c);
        var nm = ConsentMapper.INSTANCE.toMessage(c);
        assertEquals(m, nm);
    }
}
