package com.asc.registration.core.domain.event;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.core.domain.entity.Client;
import java.time.ZonedDateTime;

/**
 * ClientUpdatedEvent represents a domain event that is triggered when a client is updated. It
 * extends the {@link ClientEvent} class, inheriting its properties and behavior.
 */
public class ClientUpdatedEvent extends ClientEvent {

  /**
   * Constructs a ClientUpdatedEvent with the specified audit information, client, and event
   * timestamp.
   *
   * @param audit the audit information related to the event
   * @param client the client that was updated
   * @param eventAt the timestamp of the event
   */
  public ClientUpdatedEvent(Audit audit, Client client, ZonedDateTime eventAt) {
    super(audit, client, eventAt);
  }
}
