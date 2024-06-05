package com.asc.registration.messaging.publisher;

import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.registration.core.domain.event.ClientEvent;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.ApplicationEventPublisherAware;
import org.springframework.stereotype.Component;

/**
 * ClientApplicationDomainEventPublisher is responsible for publishing client domain events within
 * the Spring application context.
 *
 * <p>This class implements {@link ApplicationEventPublisherAware} and {@link
 * DomainEventPublisher<ClientEvent>}. It uses the Spring {@link ApplicationEventPublisher} to
 * publish events and logs the publishing process.
 */
@Slf4j
@Component
public class ClientApplicationDomainEventPublisher
    implements ApplicationEventPublisherAware, DomainEventPublisher<ClientEvent> {
  private ApplicationEventPublisher applicationEventPublisher;

  /**
   * Sets the {@link ApplicationEventPublisher} that this object runs in.
   *
   * @param applicationEventPublisher the {@link ApplicationEventPublisher} to be used by this
   *     object
   */
  public void setApplicationEventPublisher(ApplicationEventPublisher applicationEventPublisher) {
    this.applicationEventPublisher = applicationEventPublisher;
  }

  /**
   * Publishes the given client event within the Spring application context.
   *
   * <p>This method also logs the user ID and action code from the event's audit information using
   * the MDC (Mapped Diagnostic Context).
   *
   * @param event the client event to be published
   */
  public void publish(ClientEvent event) {
    applicationEventPublisher.publishEvent(event);
    MDC.put("user_id", event.getAudit().getUserId());
    MDC.put("action", String.valueOf(event.getAudit().getAuditCode().getCode()));
    log.info("Client event has been published inside Spring application");
    MDC.clear();
  }
}
