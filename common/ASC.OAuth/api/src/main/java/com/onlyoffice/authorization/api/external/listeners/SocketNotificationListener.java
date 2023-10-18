/**
 *
 */
package com.onlyoffice.authorization.api.external.listeners;

import com.corundumstudio.socketio.SocketIOServer;
import com.onlyoffice.authorization.api.core.transfer.messages.NotificationMessage;
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
@Component
@RequiredArgsConstructor
@RabbitListener(queues = "#{rabbitMQConfiguration.getSocket().getQueue()}")
@Slf4j
public class SocketNotificationListener {
    private final SocketIOServer server;
    @RabbitHandler
    public void receiveConfirmation(
            @Payload NotificationMessage message,
            Channel channel,
            @Header(AmqpHeaders.DELIVERY_TAG) long tag
    ) {
        server.getRoomOperations(String.valueOf(message.getTenant())).getClients().forEach(c -> {
            c.sendEvent("client_created", message.getClientId());
        });
    }
}
