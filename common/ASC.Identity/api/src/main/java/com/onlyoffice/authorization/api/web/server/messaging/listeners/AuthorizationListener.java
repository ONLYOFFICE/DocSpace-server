/**
 *
 */
package com.onlyoffice.authorization.api.web.server.messaging.listeners;

import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

/**
 *
 */
@Component
@RabbitListener(
        queues = "${spring.cloud.messaging.rabbitmq.queues.authorization.queue}",
        containerFactory = "prefetchRabbitListenerContainerFactory"
)
public class AuthorizationListener extends MessageListener<AuthorizationMessage> {}
