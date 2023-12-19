/**
 *
 */
package com.onlyoffice.authorization.core.transfer.messaging;

import lombok.*;

import java.io.Serializable;
import java.time.ZonedDateTime;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class ConsentMessage implements Serializable {
    private String registeredClientId;
    private String principalName;
    private String scopes;
    private ZonedDateTime modifiedAt;
    private Boolean invalidated;
}
