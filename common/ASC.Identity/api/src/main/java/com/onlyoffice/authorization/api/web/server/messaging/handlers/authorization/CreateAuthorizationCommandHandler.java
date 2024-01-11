package com.onlyoffice.authorization.api.web.server.messaging.handlers.authorization;

import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCreationUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.handlers.MessagingCommandHandler;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
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
final class CreateAuthorizationCommandHandler implements MessagingCommandHandler<AuthorizationMessage> {
    private final AuthorizationCreationUsecases creationUsecases;

    /**
     *
     * @param messages
     * @param channel
     */
    public void handle(List<Message<AuthorizationMessage>> messages, Channel channel) {
        if (messages.size() > 0) {
            MDC.put("messagesCount", String.valueOf(messages.size()));
            log.debug("Persisting authorization messages");
            MDC.clear();

            try {
                var ids = creationUsecases.saveAuthorizations(messages
                        .stream().map(s -> s.getPayload())
                        .collect(Collectors.toSet()));

                messages.forEach(m -> {
                    var payload = m.getPayload();
                    var dTag = m.getHeaders().get(AmqpHeaders.DELIVERY_TAG);
                    if (dTag instanceof Long tag) {
                        try {
                            MDC.put("id", payload.getId());
                            log.debug("Trying to ack/nack an authorization message");
                            if (!ids.contains(payload.getId()))
                                channel.basicAck(tag, true);
                            else
                                channel.basicNack(tag, false, true);
                        } catch (IOException e) {
                            log.error("Could not send client ack/nack signal to the broker", e);
                        } finally {
                            MDC.clear();
                        }
                    }
                });
            } catch (RuntimeException e) {
                log.error("Could not persist a batch of authorizations due to timeout");
                messages.forEach(m -> {
                    var dTag = m.getHeaders().get(AmqpHeaders.DELIVERY_TAG);
                    if (dTag != null && dTag instanceof Long tag) {
                        try {
                            MDC.put("id", m.getPayload().getId());
                            log.debug("Resending authorization message");
                            channel.basicNack(tag, false, true);
                        } catch (IOException ioe) {
                            log.error("Could not send authorization's ack/nack signal to the broker", ioe);
                        } finally {
                            MDC.clear();
                        }
                    }
                });
            }
        }
    }

    /**
     *
     * @return
     */
    public String getCode() {
        return AuthorizationMessage.AuthorizationCommandCode.CREATE_AUTHORIZATION.name();
    }
}
