/**
 *
 */
package com.onlyoffice.authorization.api.configuration;

import com.corundumstudio.socketio.SocketIOServer;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.stereotype.Component;

/**
 *
 */
@Component
@Slf4j
@RequiredArgsConstructor
public class SocketCommandLineRunner implements CommandLineRunner {
    private final SocketIOServer server;

    /**
     *
     * @param args
     * @throws Exception
     */
    public void run(String... args) throws Exception {
        server.start();
    }
}
