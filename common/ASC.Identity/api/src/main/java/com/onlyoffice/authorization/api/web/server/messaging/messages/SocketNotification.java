/**
 *
 */
package com.onlyoffice.authorization.api.web.server.messaging.messages;

import lombok.*;

import java.io.Serializable;

/**
 *
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class SocketNotification implements Serializable {
    private String clientId;
    private int tenant;
}
