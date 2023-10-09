package com.onlyoffice.authorization.api.messaging.listeners;

import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.messaging.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.messaging.messages.wrappers.MessageWrapper;
import com.onlyoffice.authorization.api.usecases.service.authorization.AuthorizationCreationUsecases;
import com.rabbitmq.client.Channel;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitHandler;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.stream.Collectors;

@Component
@RequiredArgsConstructor
@RabbitListener(
        queues = "${messaging.rabbitmq.configuration.authorization.queue}",
        containerFactory = "prefetchRabbitListenerContainerFactory"
)
@Slf4j
public class AuthorizationListener {
    private final RabbitMQConfiguration configuration;
    private final AuthorizationCreationUsecases creationUsecases;
    @Getter
    private LinkedBlockingQueue<MessageWrapper<AuthorizationMessage>> messages = new LinkedBlockingQueue<>();
    @RabbitHandler
    public void receiveMessage(
            @Payload AuthorizationMessage message,
            Channel channel,
            @Header(AmqpHeaders.DELIVERY_TAG) long tag
    ) {
        if (messages.size() > configuration.getPrefetch()) {
            log.warn("authorization message queue is full");
            return;
        }

        log.info("adding an authorization message to the queue");

        messages.add(MessageWrapper.
                <AuthorizationMessage>builder()
                .tag(tag)
                .channel(channel)
                .data(message)
                .build());
    }

    @Scheduled(fixedDelay = 1000)
    private void persistAuthorizations() {
        if (messages.size() > 0) {
            log.info("persisting authorization messages (count {})", messages.size());

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
                } catch (IOException e) {} finally {
                    return true;
                }
            });
        }
    }
}
