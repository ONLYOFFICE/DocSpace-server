package com.onlyoffice.authorization.api.web.server.messaging.handlers;

import com.onlyoffice.authorization.api.web.server.messaging.messages.MessageWrapper;
import org.springframework.beans.factory.annotation.Autowired;

public interface MessagingCommandHandler<E> {
    void handle(MessageWrapper<E> message);
    String getCode();
    @Autowired
    default void selfRegistration(MessagingCommandHandlerRegistry registry) {
        registry.register(this);
    }
}
