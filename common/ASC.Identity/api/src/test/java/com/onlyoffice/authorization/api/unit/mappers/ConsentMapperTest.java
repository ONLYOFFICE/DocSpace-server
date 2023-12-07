/**
 *
 */
package com.onlyoffice.authorization.api.unit.mappers;

import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ConsentMapper;
import com.onlyoffice.authorization.api.web.server.transfer.messages.ConsentMessage;
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
public class ConsentMapperTest {
    @Test
    void shouldMapEntityToMessage() {
        var c = Consent.builder()
                .principalName("mock")
                .registeredClientId("mock")
                .invalidated(false)
                .modifiedAt(Timestamp.from(Instant.now()))
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
                .modifiedAt(Timestamp.from(Instant.now()))
                .invalidated(false)
                .build();
        var c = ConsentMapper.INSTANCE.toEntity(m);
        assertNotNull(c);
        var nm = ConsentMapper.INSTANCE.toMessage(c);
        assertEquals(m, nm);
    }
}
