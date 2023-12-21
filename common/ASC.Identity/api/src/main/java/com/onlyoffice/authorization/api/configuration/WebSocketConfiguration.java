/**
 *
 */
package com.onlyoffice.authorization.api.configuration;

import com.corundumstudio.socketio.SocketIOServer;
import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 *
 */
@Getter
@Setter
@Configuration
@ConfigurationProperties(prefix = "server.socket")
public class WebSocketConfiguration {
    private String host = "127.0.0.1";
    private Integer port = 8585;

    /**
     *
     * @return
     */
    @Bean
    public SocketIOServer socketIOServer() {
        var config = new com.corundumstudio.socketio.Configuration();
        config.setHostname(host);
        config.setPort(port);
        return new SocketIOServer(config);
    }
}
