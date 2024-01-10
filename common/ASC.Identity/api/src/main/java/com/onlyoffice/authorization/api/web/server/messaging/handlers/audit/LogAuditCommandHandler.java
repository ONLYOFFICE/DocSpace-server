package com.onlyoffice.authorization.api.web.server.messaging.handlers.audit;

import com.onlyoffice.authorization.api.core.usecases.service.audit.AuditCreationUsecases;
import com.onlyoffice.authorization.api.web.server.messaging.handlers.ScheduledMessagingCommandHandler;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuditMessage;
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
final class LogAuditCommandHandler extends ScheduledMessagingCommandHandler<AuditMessage> {
    private final AuditCreationUsecases auditUsecases;

    public String getCode() {
        return AuditMessage.AuditCommandCode.LOG_AUDIT.name();
    }

    /**
     *
     */
    @Scheduled(fixedDelay = 1000)
    private void persistMessages() {
        if (messages.size() > 0) {
            MDC.put("messagesCount", String.valueOf(messages.size()));
            log.debug("Persisting audit messages");

            try {
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
            } catch (Exception e) {
                log.error("Could not commit audit messages transaction");
            } finally {
                MDC.clear();
            }
        }
    }
}
