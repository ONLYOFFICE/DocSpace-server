package com.onlyoffice.authorization.api.web.server.messaging.listeners;

import com.onlyoffice.authorization.api.web.server.messaging.handlers.MessagingCommandHandlerRegistry;
import com.onlyoffice.authorization.api.web.server.messaging.messages.Message;
import com.rabbitmq.client.Channel;
import org.springframework.beans.factory.annotation.Autowired;

import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

/**
 *
 * @param <P>
 */
public abstract class MessageListener<P extends Message> {
    @Autowired
    private MessagingCommandHandlerRegistry registry;

    /**
     *
     * @param messages
     * @param channel
     */
    public void receiveMessage(
            List<org.springframework.messaging.Message<P>> messages,
            Channel channel
    ) {
        Map<String, List<org.springframework.messaging.Message<P>>> groups = messages.stream()
                .collect(Collectors.groupingBy(m -> m.getPayload().getCode()));

        groups.forEach((code, group) -> {
            var command = registry.getCommand(code);
            if (command.isPresent())
                command.get().handle(group, channel);
        });
    }
}
