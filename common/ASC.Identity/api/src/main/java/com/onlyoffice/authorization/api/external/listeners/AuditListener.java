package com.onlyoffice.authorization.api.external.listeners;

import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.transfer.messages.AuditMessage;
import com.onlyoffice.authorization.api.core.transfer.messages.wrappers.MessageWrapper;
import com.onlyoffice.authorization.api.core.usecases.service.audit.AuditCreationUsecases;
import com.rabbitmq.client.Channel;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.rabbit.annotation.RabbitHandler;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.util.UUID;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.stream.Collectors;

@Component
@RequiredArgsConstructor
@RabbitListener(
        queues = "${messaging.rabbitmq.configuration.audit.queue}",
        containerFactory = "prefetchRabbitListenerContainerFactory"
)
@Slf4j
public class AuditListener {
    private final RabbitMQConfiguration configuration;
    private final AuditCreationUsecases auditUsecases;
    @Getter
    private LinkedBlockingQueue<MessageWrapper<AuditMessage>> messages = new LinkedBlockingQueue<>();
    @RabbitHandler
    public void receiveMessage(
            @Payload AuditMessage message,
            Channel channel,
            @Header(AmqpHeaders.DELIVERY_TAG) long tag
    ) {
        if (messages.size() > configuration.getPrefetch()) {
            log.warn("Audit message queue is full");
            return;
        }

        log.info("Adding an audit message to the queue");
        message.setTag(UUID.randomUUID().toString());

        messages.add(MessageWrapper.
                <AuditMessage>builder()
                .tag(tag)
                .channel(channel)
                .data(message)
                .build());
    }

    @Scheduled(fixedDelay = 1000)
    private void persistAudits() {
        if (messages.size() > 0) {
            MDC.put("number of messages", String.valueOf(messages.size()));
            log.info("Persisting audit messages");
            MDC.clear();

            var ids = auditUsecases.saveAudits(messages
                    .stream().map(s -> s.getData())
                    .collect(Collectors.toSet()));

            messages.removeIf(m -> {
                var tag = m.getTag();
                var channel = m.getChannel();

                try {
                    if (!ids.contains(m.getData().getTag()))
                        channel.basicAck(tag, true);
                    else
                        channel.basicNack(tag, false, true);
                } catch (IOException e) {
                    log.error("Could not persist audits", e);
                } finally {
                    return true;
                }
            });
        }
    }
}
