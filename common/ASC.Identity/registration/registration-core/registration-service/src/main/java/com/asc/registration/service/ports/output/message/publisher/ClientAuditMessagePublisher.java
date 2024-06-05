package com.asc.registration.service.ports.output.message.publisher;

import com.asc.common.service.transfer.message.AuditMessage;

/**
 * ClientAuditMessagePublisher defines the contract for publishing audit messages. This interface
 * handles the publishing of audit messages related to client operations.
 */
public interface ClientAuditMessagePublisher {
  /**
   * Publishes an audit message.
   *
   * @param message The audit message to be published.
   */
  void publish(AuditMessage message);
}
