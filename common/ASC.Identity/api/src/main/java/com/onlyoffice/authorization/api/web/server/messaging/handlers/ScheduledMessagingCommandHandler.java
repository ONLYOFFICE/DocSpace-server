package com.onlyoffice.authorization.api.web.server.messaging.handlers;

import com.onlyoffice.authorization.api.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.web.server.messaging.messages.MessageWrapper;
import lombok.extern.slf4j.Slf4j;

import java.util.concurrent.LinkedBlockingQueue;

/**
 *
 * @param <E>
 */
@Slf4j
public abstract class ScheduledMessagingCommandHandler<E> implements MessagingCommandHandler<E> {
    private RabbitMQConfiguration configuration = new RabbitMQConfiguration();
    protected LinkedBlockingQueue<MessageWrapper<E>> messages = new LinkedBlockingQueue<>();

    /**
     *
     * @param message
     */
    public void handle(MessageWrapper<E> message) {
        if (messages.size() > configuration.getPrefetch()) {
            log.warn("Message queue is full");
            return;
        }

        log.info("Adding a message to the queue");
        messages.add(message);
    }

    public abstract String getCode();
}
