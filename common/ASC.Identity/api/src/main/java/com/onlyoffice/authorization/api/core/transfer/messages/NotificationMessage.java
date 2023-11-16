/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.messages;

import lombok.*;

import java.io.Serializable;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class NotificationMessage implements Serializable {
    private String clientId;
    private int tenant;
}
