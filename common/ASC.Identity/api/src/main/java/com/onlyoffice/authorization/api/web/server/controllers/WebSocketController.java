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

    public WebSocketController(SocketIOServer srv) {
        server = srv;
        server.addConnectListener(onConnected());
        server.addDisconnectListener(onDisconnected());
    }

    /**
     *
     * @return
     */
    private ConnectListener onConnected() {
        return (client) -> {
            var tenant = client.getHandshakeData().getSingleUrlParam("tenant");
            if (tenant == null || tenant.isBlank()) {
                client.disconnect();
                return;
            }
            client.joinRoom(tenant);
            log.info("Client[{}] - Connected to socket", client.getSessionId().toString());
        };
    }

    /**
     *
     * @return
     */
    private DisconnectListener onDisconnected() {
        return client -> {
            log.info("Client[{}] - Disconnected from socket", client.getSessionId().toString());
        };
    }
}