package com.asc.registration.messaging.listener;

import com.asc.common.service.AuditCreateCommandHandler;
import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.registration.messaging.mapper.AuditDataMapper;
import com.rabbitmq.client.Channel;
import java.util.List;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.messaging.Message;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Component;

/** RabbitClientAuditMessageListener listens for audit messages from RabbitMQ and processes them. */
@Slf4j
@Component
@RequiredArgsConstructor
public class RabbitClientAuditMessageListener {
  private final AuditCreateCommandHandler auditCreateCommandHandler;
  private final AuditDataMapper auditDataMapper;

  /**
   * Receives and processes audit messages from RabbitMQ.
   *
   * @param messages The list of audit messages.
   * @param channel The RabbitMQ channel.
   */
  @RabbitListener(
      queues = "${spring.cloud.messaging.rabbitmq.queues.audit.queue}",
      containerFactory = "batchRabbitListenerContainerFactory")
  public void receiveMessage(@Payload List<Message<AuditMessage>> messages, Channel channel) {
    if (!messages.isEmpty()) {
      MDC.put("count", String.valueOf(messages.size()));
      log.debug("Persisting audit messages");
      MDC.clear();

      auditCreateCommandHandler.createAudits(
          messages.stream()
              .map(s -> auditDataMapper.toAudit(s.getPayload()))
              .collect(Collectors.toSet()));
    }
  }
}
