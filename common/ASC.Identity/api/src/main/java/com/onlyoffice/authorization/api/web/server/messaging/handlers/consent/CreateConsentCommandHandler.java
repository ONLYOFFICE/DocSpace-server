package com.onlyoffice.authorization.api.web.server.messaging.handlers.consent;

import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCreationUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.handlers.ScheduledMessagingCommandHandler;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ConsentMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.util.stream.Collectors;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
final class CreateConsentCommandHandler extends ScheduledMessagingCommandHandler<ConsentMessage> {
    private final ConsentCreationUsecases creationUsecases;

    public String getCode() {
        return ConsentMessage.ConsentCommandCode.CREATE_CONSENT.name();
    }

    /**
     *
     */
    @Scheduled(fixedDelay = 1000)
    private void persistMessages() {
        if (messages.size() > 0) {
            MDC.put("messagesCount", String.valueOf(messages.size()));
            log.debug("Persisting consent messages");
            MDC.clear();

            creationUsecases.saveConsents(messages
                    .stream().map(s -> s.getData())
                    .collect(Collectors.toSet()));

            messages.removeIf(m -> {
                var tag = m.getTag();
                var channel = m.getChannel();

                try {
                    channel.basicAck(tag, true);
                } catch (IOException e) {
                    log.error("Could not persist consents", e);
                } finally {
                    return true;
                }
            });
        }
    }
}
