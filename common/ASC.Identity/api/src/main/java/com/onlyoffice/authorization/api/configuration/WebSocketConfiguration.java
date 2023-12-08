/**
 *
 */
package com.onlyoffice.authorization.api.configuration;

import com.corundumstudio.socketio.SocketIOServer;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 *
 */
@Configuration
public class WebSocketConfiguration {
    @Value("${server.socket.host}")
    private String host;
    @Value("${server.socket.port}")
    private Integer port;

    @Bean
    public SocketIOServer socketIOServer() {
        var config = new com.corundumstudio.socketio.Configuration();
        config.setHostname(host);
        config.setPort(port);
        return new SocketIOServer(config);
    }
}
