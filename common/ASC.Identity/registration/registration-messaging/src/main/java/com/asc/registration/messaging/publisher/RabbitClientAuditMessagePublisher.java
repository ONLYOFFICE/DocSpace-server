package com.asc.registration.messaging.publisher;

import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.registration.messaging.configuration.RabbitMQConfiguration;
import com.asc.registration.service.ports.output.message.publisher.ClientAuditMessagePublisher;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.stereotype.Component;

/**
 * RabbitClientAuditMessagePublisher publishes audit messages to RabbitMQ.
 *
 * <p>This class implements the {@link ClientAuditMessagePublisher} interface and uses RabbitMQ to
 * send audit messages. It logs the message publishing process and handles any exceptions that occur
 * during message sending.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class RabbitClientAuditMessagePublisher implements ClientAuditMessagePublisher {
  private final RabbitMQConfiguration configuration;
  private final AmqpTemplate amqpClient;

  /**
   * Publishes the given audit message to RabbitMQ.
   *
   * <p>This method retrieves the RabbitMQ queue configuration for the "audit" queue, and attempts
   * to send the audit message using the {@link AmqpTemplate}. If an exception occurs during message
   * sending, it logs the error along with the action, tenant ID, and user ID from the message using
   * the MDC (Mapped Diagnostic Context).
   *
   * @param message the audit message to be published
   */
  public void publish(AuditMessage message) {
    log.debug("Sending an audit message: {}", message);

    var queue = configuration.getQueues().get("audit");

    try {
      amqpClient.convertAndSend(queue.getExchange(), queue.getRouting(), message);
    } catch (Exception e) {
      MDC.put("action", String.valueOf(message.getAction()));
      MDC.put("tenant_id", String.valueOf(message.getTenantId()));
      MDC.put("user_id", message.getUserId());
      log.error("Could not send an audit message", e);
      MDC.clear();
    }
  }
}
