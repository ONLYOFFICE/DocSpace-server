/**
 *
 */
package com.onlyoffice.authorization.api.web.server.messaging.listeners;

import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

/**
 *
 */
@Component
@RabbitListener(
        queues = "${spring.cloud.messaging.rabbitmq.queues.client.queue}",
        containerFactory = "prefetchRabbitListenerContainerFactory"
)
public class ClientListener extends MessageListener<ClientMessage> {}
