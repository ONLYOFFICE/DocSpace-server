/**
 *
 */
package com.onlyoffice.authorization.api.web.server.messaging.listeners;

import com.onlyoffice.authorization.api.web.server.messaging.messages.ConsentMessage;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

/**
 *
 */
@Component
@RabbitListener(
        queues = "${messaging.rabbitmq.configuration.consent.queue}",
        containerFactory = "prefetchRabbitListenerContainerFactory"
)
public class ConsentListener extends MessageListener<ConsentMessage> {}
