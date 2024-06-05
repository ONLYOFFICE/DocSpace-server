package com.asc.registration.messaging.listener;

import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.messaging.mapper.AuditDataMapper;
import com.asc.registration.service.ports.output.message.publisher.ClientAuditMessagePublisher;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.event.TransactionalEventListener;

/** ClientApplicationDomainEventListener listens for client domain events and processes them. */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientApplicationDomainEventListener {
  // We do not need outbox here since it is
  // not that crucial to handle all the incoming
  // audit messages
  private final ClientAuditMessagePublisher messagePublisher;
  private final AuditDataMapper auditDataMapper;

  /**
   * Processes client domain events and publishes audit messages.
   *
   * @param event The client event to process.
   */
  @TransactionalEventListener
  void process(ClientEvent event) {
    messagePublisher.publish(auditDataMapper.toMessage(event.getAudit()));
  }
}
