/**
 *
 */
package com.asc.authorization.api.web.server.messaging.listeners;

import com.asc.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import com.rabbitmq.client.Channel;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.messaging.Message;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 *
 */
@Component
public class AuthorizationListener extends MessageListener<AuthorizationMessage> {
    @RabbitListener(
            queues = "${spring.cloud.messaging.rabbitmq.queues.authorization.queue}",
            containerFactory = "batchRabbitListenerContainerFactory"
    )
    public void receiveMessage(List<Message<AuthorizationMessage>> messages, Channel channel) {
        super.receiveMessage(messages, channel);
    }
}
