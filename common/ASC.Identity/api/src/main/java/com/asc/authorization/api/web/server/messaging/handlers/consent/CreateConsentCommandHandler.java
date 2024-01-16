package com.asc.authorization.api.web.server.messaging.handlers.consent;

import com.asc.authorization.api.core.usecases.service.consent.ConsentCreationUsecases;
import com.asc.authorization.api.web.server.messaging.handlers.MessagingCommandHandler;
import com.asc.authorization.api.web.server.messaging.messages.ConsentMessage;
import com.rabbitmq.client.Channel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.Message;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.util.List;
import java.util.stream.Collectors;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
final class CreateConsentCommandHandler implements MessagingCommandHandler<ConsentMessage> {
    private final ConsentCreationUsecases creationUsecases;

    /**
     *
     * @param messages
     * @param channel
     */
    public void handle(List<Message<ConsentMessage>> messages, Channel channel) {
        if (messages.size() > 0) {
            MDC.put("messagesCount", String.valueOf(messages.size()));
            log.debug("Persisting consent messages");
            MDC.clear();

            try {
                creationUsecases.saveConsents(messages
                        .stream().map(s -> s.getPayload())
                        .collect(Collectors.toSet()));
            } catch (RuntimeException e) {
                log.error("Could not persist a batch of consents due to timeout", e);
                return;
            }

            messages.forEach(m -> {
                var dTag = m.getHeaders().get(AmqpHeaders.DELIVERY_TAG);
                if (dTag != null && dTag instanceof Long tag) {
                    var payload = m.getPayload();
                    try {
                        MDC.put("principalName", payload.getPrincipalName());
                        MDC.put("clientId", payload.getRegisteredClientId());
                        log.debug("Trying to ack a consent message");
                        channel.basicAck(tag, true);
                    } catch (IOException e) {
                        log.error("Could not send consent's ack/nack signal to the broker", e);
                    } finally {
                        MDC.clear();
                    }
                }
            });
        }
    }

    /**
     *
     * @return
     */
    public String getCode() {
        return ConsentMessage.ConsentCommandCode.CREATE_CONSENT.name();
    }
}
