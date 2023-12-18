/**
 *
 */
package com.onlyoffice.authorization.api.web.server.messaging.listeners;

import com.corundumstudio.socketio.SocketIOServer;
import com.onlyoffice.authorization.api.web.server.messaging.messages.SocketNotification;
import com.rabbitmq.client.Channel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitHandler;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Component;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
@RabbitListener(queues = "#{rabbitMQConfiguration.getSocket().getQueue()}")
public class SocketNotificationListener {
    private final String CLIENT_CHANGED_EVENT = "client_changed";

    private final SocketIOServer server;
    @RabbitHandler
    public void receiveConfirmation(
            @Payload SocketNotification message,
            Channel channel,
            @Header(AmqpHeaders.DELIVERY_TAG) long tag
    ) {
        server.getRoomOperations(String.valueOf(message.getTenant()))
                .getClients().forEach(c -> c.sendEvent(CLIENT_CHANGED_EVENT,
                        message.getClientId()));
    }
}
