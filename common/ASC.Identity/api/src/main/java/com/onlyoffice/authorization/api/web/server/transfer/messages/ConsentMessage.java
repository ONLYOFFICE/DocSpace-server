/**
 *
 */
package com.onlyoffice.authorization.api.web.server.transfer.messages;

import lombok.*;

import java.io.Serializable;
import java.sql.Timestamp;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
@EqualsAndHashCode
public class ConsentMessage implements Serializable {
    private String registeredClientId;
    private String principalName;
    private String scopes;
    private Timestamp modifiedAt;
    private Boolean invalidated;
}
