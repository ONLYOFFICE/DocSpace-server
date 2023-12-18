package com.onlyoffice.authorization.api.web.server.messaging.handlers.client;

import com.onlyoffice.authorization.api.core.usecases.service.client.ClientMutationUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.handlers.ScheduledMessagingCommandHandler;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.data.util.Pair;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.util.stream.Collectors;

@Slf4j
@Component
@RequiredArgsConstructor
final class UpdateClientCommandHandler extends ScheduledMessagingCommandHandler<ClientMessage> {
    private final ClientMutationUsecases mutationUsecases;

    public String getCode() {
        return ClientMessage.ClientCommandCode.UPDATE_CLIENT.name();
    }

    @Scheduled(fixedDelay = 1000)
    private void persistMessages() {
        if (messages.size() > 0) {
            MDC.put("number of messages", String.valueOf(messages.size()));
            log.info("Update client messages");
            MDC.clear();

            var ids = mutationUsecases.updateClients(messages
                    .stream().map(m -> Pair.of(m.getData().getClientId(), m.getData()))
                    .collect(Collectors.toSet()));

            messages.removeIf(w -> {
                var tag = w.getTag();
                var channel = w.getChannel();

                try {
                    if (!ids.contains(w.getData().getClientId()))
                        channel.basicAck(tag, true);
                    else
                        channel.basicNack(tag, false, true);
                } catch (IOException e) {
                    log.error("Could not update clients", e);
                } finally {
                    return true;
                }
            });
        }
    }
}
