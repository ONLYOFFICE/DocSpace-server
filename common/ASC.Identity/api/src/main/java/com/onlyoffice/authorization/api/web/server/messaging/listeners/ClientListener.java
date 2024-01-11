/**
 *
 */
package com.onlyoffice.authorization.api.web.server.messaging.listeners;

import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import com.rabbitmq.client.Channel;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.messaging.Message;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 *
 */
@Component
public class ClientListener extends MessageListener<ClientMessage> {
    @RabbitListener(
            queues = "${spring.cloud.messaging.rabbitmq.queues.client.queue}",
            containerFactory = "batchRabbitListenerContainerFactory"
    )
    public void receiveMessage(List<Message<ClientMessage>> messages, Channel channel) {
        super.receiveMessage(messages, channel);
    }
}
