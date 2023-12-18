package com.onlyoffice.authorization.api.web.server.messaging.handlers;

import org.springframework.stereotype.Component;

import java.util.HashMap;
import java.util.Map;
import java.util.Optional;

@Component
public class MessagingCommandHandlerHandlerRegistryContainer implements MessagingCommandHandlerRegistry {
    private final Map<String, MessagingCommandHandler> registry = new HashMap<>();

    public void register(MessagingCommandHandler command) {
        registry.putIfAbsent(command.getCode(), command);
    }

    public Optional<MessagingCommandHandler> getCommand(String code) {
        return Optional.of(registry.get(code));
    }
}
