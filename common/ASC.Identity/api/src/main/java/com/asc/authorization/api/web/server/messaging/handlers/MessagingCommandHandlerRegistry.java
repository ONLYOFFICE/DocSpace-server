package com.asc.authorization.api.web.server.messaging.handlers;

import java.util.Optional;

/**
 *
 */
public interface MessagingCommandHandlerRegistry {
    /**
     *
     * @param command
     */
    void register(MessagingCommandHandler command);

    /**
     *
     * @param code
     * @return
     */
    Optional<MessagingCommandHandler> getCommand(String code);
}
