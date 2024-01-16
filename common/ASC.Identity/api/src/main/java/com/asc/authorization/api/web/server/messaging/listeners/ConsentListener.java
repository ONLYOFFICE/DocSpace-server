/**
 *
 */
package com.asc.authorization.api.web.server.messaging.listeners;

import com.asc.authorization.api.web.server.messaging.messages.ConsentMessage;
import com.rabbitmq.client.Channel;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.messaging.Message;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 *
 */
@Component
public class ConsentListener extends MessageListener<ConsentMessage> {
    @RabbitListener(
            queues = "${spring.cloud.messaging.rabbitmq.queues.consent.queue}",
            containerFactory = "batchRabbitListenerContainerFactory"
    )
    public void receiveMessage(List<Message<ConsentMessage>> messages, Channel channel) {
        super.receiveMessage(messages, channel);
    }
}
