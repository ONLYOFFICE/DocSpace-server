package com.onlyoffice.authorization.api.web.server.messaging.handlers;

import java.util.Optional;

public interface MessagingCommandHandlerRegistry {
    void register(MessagingCommandHandler command);
    Optional<MessagingCommandHandler> getCommand(String code);
}
