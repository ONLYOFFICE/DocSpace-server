/**
 *
 */
package com.onlyoffice.authorization.api.web.server.controllers;

import com.corundumstudio.socketio.SocketIOServer;
import com.corundumstudio.socketio.listener.ConnectListener;
import com.corundumstudio.socketio.listener.DisconnectListener;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

/**
 *
 */
@Component
@Slf4j
public class WebSocketController {
    private final SocketIOServer server;

    public WebSocketController(SocketIOServer server) {
        this.server = server;
        server.addConnectListener(onConnected());
        server.addDisconnectListener(onDisconnected());
    }

    private ConnectListener onConnected() {
        return (client) -> {
            String tenant = client.getHandshakeData().getSingleUrlParam("tenant");
            if (tenant == null || tenant.isBlank()) {
                client.disconnect();
                return;
            }
            client.joinRoom(tenant);
            log.info("client[{}] - Connected to socket", client.getSessionId().toString());
        };
    }

    private DisconnectListener onDisconnected() {
        return client -> {
            log.info("client[{}] - Disconnected from socket", client.getSessionId().toString());
        };
    }
}
