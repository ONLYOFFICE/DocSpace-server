package com.onlyoffice.authorization.api.web.server.messaging.handlers;

import com.rabbitmq.client.Channel;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.messaging.Message;

import java.util.List;

/**
 *
 * @param <E>
 */
public interface MessagingCommandHandler<E> {
    /**
     *
     * @param messages
     * @param channel
     */
    void handle(List<Message<E>> messages, Channel channel);

    /**
     *
     * @return
     */
    String getCode();

    /**
     *
     * @param registry
     */
    @Autowired
    default void selfRegistration(MessagingCommandHandlerRegistry registry) {
        registry.register(this);
    }
}
