package com.onlyoffice.authorization.api.web.server.messaging.handlers.client;

import com.onlyoffice.authorization.api.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCreationUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.handlers.ScheduledMessagingCommandHandler;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import com.onlyoffice.authorization.api.web.server.messaging.messages.SocketNotification;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
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
final class CreateClientCommandHandler extends ScheduledMessagingCommandHandler<ClientMessage> {
    private final RabbitMQConfiguration configuration;
    private final ClientCreationUsecases creationUsecases;
    private final AmqpTemplate amqpClient;

    public String getCode() {
        return ClientMessage.ClientCommandCode.CREATE_CLIENT.name();
    }

    /**
     *
     */
    @Scheduled(fixedDelay = 1000)
    private void persistMessages() {
        if (messages.size() > 0) {
            MDC.put("messagesCount", String.valueOf(messages.size()));
            log.debug("Persisting client messages");
            MDC.clear();

            var ids = creationUsecases.saveClients(messages.stream()
                    .map(msg -> msg.getData())
                    .collect(Collectors.toList()));
            var queue = configuration.getQueues().get("socket");

            messages.removeIf(w -> {
                var tag = w.getTag();
                var channel = w.getChannel();

                try {
                    if (!ids.contains(w.getData().getClientId())) {
                        channel.basicAck(tag, true);
                        amqpClient.convertAndSend(
                                queue.getExchange(),
                                "",
                                SocketNotification.builder()
                                        .tenant(w.getData().getTenant())
                                        .clientId(w.getData().getClientId())
                                        .build()
                        );
                    } else
                        channel.basicNack(tag, false, true);
                } catch (IOException e) {
                    log.error("Could not persist clients", e);
                } finally {
                    return true;
                }
            });
        }
    }
}
