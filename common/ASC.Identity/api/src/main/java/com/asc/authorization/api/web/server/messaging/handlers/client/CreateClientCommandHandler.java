package com.asc.authorization.api.web.server.messaging.handlers.client;

import com.asc.authorization.api.core.usecases.service.client.ClientCreationUsecases;
import com.asc.authorization.api.web.server.messaging.messages.ClientMessage;
import com.asc.authorization.api.web.server.messaging.handlers.MessagingCommandHandler;
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
final class CreateClientCommandHandler implements MessagingCommandHandler<ClientMessage> {
    private final ClientCreationUsecases creationUsecases;

    /**
     *
     * @param messages
     * @param channel
     */
    public void handle(List<Message<ClientMessage>> messages, Channel channel) {
        if (messages.size() > 0) {
            MDC.put("messagesCount", String.valueOf(messages.size()));
            log.debug("Persisting client messages");
            MDC.clear();

            try {
                var ids = creationUsecases.saveClients(messages.stream()
                        .map(msg -> msg.getPayload())
                        .collect(Collectors.toList()));

                messages.forEach(m -> {
                    var payload = m.getPayload();
                    var dTag = m.getHeaders().get(AmqpHeaders.DELIVERY_TAG);
                    if (dTag != null && dTag instanceof Long tag) {
                        try {
                            MDC.put("clientId", payload.getClientId());
                            log.debug("Trying to ack/nack a client message");
                            if (!ids.contains(payload.getClientId()))
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
                log.error("Could not persist a batch of clients due to timeout");
                messages.forEach(m -> {
                    var dTag = m.getHeaders().get(AmqpHeaders.DELIVERY_TAG);
                    if (dTag != null && dTag instanceof Long tag) {
                        MDC.put("clientId", m.getPayload().getClientId());
                        try {
                            log.debug("Resending client message");
                            channel.basicNack(tag, false, true);
                        } catch (IOException ioe) {
                            log.error("Could not send client's ack/nack signal to the broker", ioe);
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
        return ClientMessage.ClientCommandCode.CREATE_CLIENT.name();
    }
}
