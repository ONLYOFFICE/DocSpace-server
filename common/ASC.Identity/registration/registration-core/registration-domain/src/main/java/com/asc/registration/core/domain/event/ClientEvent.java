package com.asc.registration.core.domain.event;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEvent;
import com.asc.registration.core.domain.entity.Client;
import java.time.ZonedDateTime;

/**
 * ClientEvent is an abstract class that represents a domain event associated with a Client. It
 * encapsulates audit information, the client involved in the event, and the timestamp of the event.
 */
public abstract class ClientEvent implements DomainEvent<Client> {
  private final Audit audit;
  private final Client client;
  private final ZonedDateTime eventAt;

  /**
   * Constructs a ClientEvent with the specified audit information, client, and event timestamp.
   *
   * @param audit the audit information related to the event
   * @param client the client involved in the event
   * @param eventAt the timestamp of the event
   */
  public ClientEvent(Audit audit, Client client, ZonedDateTime eventAt) {
    this.audit = audit;
    this.client = client;
    this.eventAt = eventAt;
  }

  /**
   * Returns the audit information associated with this event.
   *
   * @return the audit information
   */
  public Audit getAudit() {
    return audit;
  }

  /**
   * Returns the client involved in this event.
   *
   * @return the client
   */
  public Client getClient() {
    return client;
  }

  /**
   * Returns the timestamp of this event.
   *
   * @return the event timestamp
   */
  public ZonedDateTime getEventAt() {
    return eventAt;
  }
}
