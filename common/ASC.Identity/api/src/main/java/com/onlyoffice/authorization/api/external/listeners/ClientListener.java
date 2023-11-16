/**
 *
 */
package com.onlyoffice.authorization.api.external.listeners;

import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.transfer.messages.ClientMessage;
import com.onlyoffice.authorization.api.core.transfer.messages.NotificationMessage;
import com.onlyoffice.authorization.api.core.transfer.messages.wrappers.MessageWrapper;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCreationUsecases;
import com.rabbitmq.client.Channel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.stream.Collectors;

/**
 *
 */
@Component
@RequiredArgsConstructor
@Slf4j
public class ClientListener {
    private final RabbitMQConfiguration configuration;

    private final ClientCreationUsecases creationUsecases;
    private final AmqpTemplate rabbitClient;

    private LinkedBlockingQueue<MessageWrapper<ClientMessage>> messages = new LinkedBlockingQueue<>();
    private AtomicInteger lastBatchProcessed = new AtomicInteger(0);

    @RabbitListener(
            queues = "${messaging.rabbitmq.configuration.client.queue}",
            containerFactory = "prefetchRabbitListenerContainerFactory"
    )
    public void receiveMessage(
            @Payload ClientMessage message,
            Channel channel,
            @Header(AmqpHeaders.DELIVERY_TAG) long tag
    ) {
        if (messages.size() == configuration.getPrefetch()) {
            log.warn("Client message queue is full");
            return;
        }

        log.info("Adding a client message to the queue");

        messages.add(MessageWrapper.
                <ClientMessage>builder().
                tag(tag).
                channel(channel).
                data(message).
                build());
    }

    public int getLastBatchSize() {
        return this.lastBatchProcessed.get();
    }

    @Scheduled(fixedDelay = 1000)
    private void persistClients() {
        if (messages.size() > 0) {
            lastBatchProcessed.set(messages.size());
            MDC.put("number of messages", String.valueOf(messages.size()));
            log.info("Persisting client messages");
            MDC.clear();

            var ids = creationUsecases.saveClients(messages.stream()
                    .map(msg -> msg.getData())
                    .collect(Collectors.toList()));

            messages.removeIf(w -> {
                var tag = w.getTag();
                var channel = w.getChannel();

                try {
                    if (!ids.contains(w.getData().getClientId())) {
                        channel.basicAck(tag, true);
                        rabbitClient.convertAndSend(
                                configuration.getSocket().getExchange(),
                                "",
                                NotificationMessage.builder()
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
