package com.asc.authorization.api.web.server.messaging.handlers.audit;

import com.asc.authorization.api.core.usecases.service.audit.AuditCreationUsecases;
import com.asc.authorization.api.web.server.messaging.messages.AuditMessage;
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
final class LogAuditCommandHandler implements MessagingCommandHandler<AuditMessage> {
    private final AuditCreationUsecases auditUsecases;

    /**
     *
     * @param messages
     * @param channel
     */
    public void handle(List<Message<AuditMessage>> messages, Channel channel) {
        if (messages.size() > 0) {
            MDC.put("messagesCount", String.valueOf(messages.size()));
            log.debug("Persisting audit messages");
            MDC.clear();

            try {
                var ids = auditUsecases.saveAudits(messages
                        .stream().map(s -> s.getPayload())
                        .collect(Collectors.toSet()));

                messages.forEach(m -> {
                    var dTag = m.getHeaders().get(AmqpHeaders.DELIVERY_TAG);
                    if (dTag instanceof Long tag) {
                        try {
                            if (!ids.contains(m.getPayload().getTag()))
                                channel.basicAck(tag, true);
                            else
                                channel.basicNack(tag, false, true);
                        } catch (IOException e) {
                            log.error("Could not send audit ack/nack signal to the broker", e);
                        }
                    }
                });
            } catch (RuntimeException e) {
                log.error("Could not persist a batch of audits due to timeout");
            }
        }
    }

    /**
     *
     * @return
     */
    public String getCode() {
        return AuditMessage.AuditCommandCode.LOG_AUDIT.name();
    }
}
