package com.asc.registration.core.domain.event;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.core.domain.entity.Client;
import java.time.ZonedDateTime;

/**
 * ClientDeletedEvent represents a domain event that is triggered when a client is deleted. It
 * extends the {@link ClientEvent} class, inheriting its properties and behavior.
 */
public class ClientDeletedEvent extends ClientEvent {

  /**
   * Constructs a ClientDeletedEvent with the specified audit information, client, and event
   * timestamp.
   *
   * @param audit the audit information related to the event
   * @param client the client that was deleted
   * @param eventAt the timestamp of the event
   */
  public ClientDeletedEvent(Audit audit, Client client, ZonedDateTime eventAt) {
    super(audit, client, eventAt);
  }
}
