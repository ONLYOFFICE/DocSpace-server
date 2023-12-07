/**
 *
 */
package com.onlyoffice.authorization.api.web.server.transfer.messages;

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
