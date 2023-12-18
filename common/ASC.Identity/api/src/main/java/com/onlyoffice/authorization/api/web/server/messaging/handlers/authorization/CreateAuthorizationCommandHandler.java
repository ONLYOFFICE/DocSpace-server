package com.onlyoffice.authorization.api.web.server.messaging.handlers.authorization;

import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCreationUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.handlers.ScheduledMessagingCommandHandler;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.util.stream.Collectors;

@Slf4j
@Component
@RequiredArgsConstructor
final class CreateAuthorizationCommandHandler extends ScheduledMessagingCommandHandler<AuthorizationMessage> {
    private final AuthorizationCreationUsecases creationUsecases;

    public String getCode() {
        return AuthorizationMessage.AuthorizationCommandCode.CREATE_AUTHORIZATION.name();
    }

    @Scheduled(fixedDelay = 1000)
    private void persistMessages() {
        if (messages.size() > 0) {
            MDC.put("number of messages", String.valueOf(messages.size()));
            log.info("Persisting authorization messages");
            MDC.clear();

            var ids = creationUsecases.saveAuthorizations(messages
                    .stream().map(s -> s.getData())
                    .collect(Collectors.toSet()));

            messages.removeIf(m -> {
                var tag = m.getTag();
                var channel = m.getChannel();

                try {
                    if (!ids.contains(m.getData().getId()))
                        channel.basicAck(tag, true);
                    else
                        channel.basicNack(tag, false, true);
                } catch (IOException e) {
                    log.error("Could not persist authorizations", e);
                } finally {
                    return true;
                }
            });
        }
    }
}
