package com.onlyoffice.authorization.api.web.server.messaging.listeners;

import com.onlyoffice.authorization.api.web.server.messaging.messages.AuditMessage;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Component
@RabbitListener(
        queues = "${messaging.rabbitmq.configuration.audit.queue}",
        containerFactory = "prefetchRabbitListenerContainerFactory"
)
public class AuditListener extends MessageListener<AuditMessage> {}
