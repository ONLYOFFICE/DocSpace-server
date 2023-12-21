package com.onlyoffice.authorization.api.web.server.messaging.handlers;

import com.onlyoffice.authorization.api.web.server.messaging.messages.MessageWrapper;
import org.springframework.beans.factory.annotation.Autowired;

/**
 *
 * @param <E>
 */
public interface MessagingCommandHandler<E> {
    /**
     *
     * @param message
     */
    void handle(MessageWrapper<E> message);

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
