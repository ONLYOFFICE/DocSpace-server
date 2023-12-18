package com.onlyoffice.authorization.api.web.server.messaging.listeners;

import com.onlyoffice.authorization.api.web.server.messaging.handlers.MessagingCommandHandlerRegistry;
import com.onlyoffice.authorization.api.web.server.messaging.messages.Message;
import com.onlyoffice.authorization.api.web.server.messaging.messages.MessageWrapper;
import com.rabbitmq.client.Channel;
import org.springframework.amqp.rabbit.annotation.RabbitHandler;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;

public abstract class MessageListener<P extends Message> {
    @Autowired
    private MessagingCommandHandlerRegistry registry;
    @RabbitHandler
    public void receiveMessage(
            @Payload P message,
            Channel channel,
            @Header(AmqpHeaders.DELIVERY_TAG) long tag
    ) {
        var command = registry.getCommand(message.getCode());
        if (command.isPresent())
            command.get().handle(MessageWrapper
                    .builder()
                    .channel(channel)
                    .tag(tag)
                    .data(message)
                    .build());
    }
}
