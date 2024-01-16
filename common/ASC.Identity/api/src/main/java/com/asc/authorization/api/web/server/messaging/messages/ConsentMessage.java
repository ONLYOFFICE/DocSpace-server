/**
 *
 */
package com.asc.authorization.api.web.server.messaging.messages;

import lombok.*;

import java.io.Serializable;
import java.time.ZonedDateTime;

/**
 *
 */
@Getter
@Setter
@Builder
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
public class ConsentMessage implements Message, Serializable {
    private String registeredClientId;
    private String principalName;
    private String scopes;
    @Builder.Default
    private ZonedDateTime modifiedAt = ZonedDateTime.now();
    private Boolean invalidated;
    @Builder.Default
    private ConsentCommandCode commandCode = ConsentCommandCode.CREATE_CONSENT;
    public String getCode() {
        return commandCode.name();
    }
    public enum ConsentCommandCode {
        CREATE_CONSENT
    }
}
