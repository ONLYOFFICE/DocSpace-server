package com.asc.authorization.api.web.server.messaging.listeners;

import com.asc.authorization.api.web.server.messaging.messages.AuditMessage;
import com.rabbitmq.client.Channel;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.messaging.Message;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 *
 */
@Component
public class AuditListener extends MessageListener<AuditMessage> {
    @RabbitListener(
            queues = "${spring.cloud.messaging.rabbitmq.queues.audit.queue}",
            containerFactory = "batchRabbitListenerContainerFactory"
    )
    public void receiveMessage(
            @Payload List<Message<AuditMessage>> messages,
            Channel channel
    ) {
        super.receiveMessage(messages, channel);
    }
}
