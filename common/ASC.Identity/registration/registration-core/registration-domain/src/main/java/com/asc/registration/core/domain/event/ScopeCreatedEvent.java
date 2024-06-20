package com.asc.registration.core.domain.event;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.core.domain.entity.Scope;
import java.time.ZonedDateTime;

/** Represents an event indicating that a {@link Scope} has been created. */
public class ScopeCreatedEvent extends ScopeEvent {
  /**
   * Constructs a ScopeCreatedEvent instance.
   *
   * @param audit the audit information related to the event
   * @param scope the scope that has been created
   * @param eventAt the timestamp when the event occurred
   */
  public ScopeCreatedEvent(Audit audit, Scope scope, ZonedDateTime eventAt) {
    super(audit, scope, eventAt);
  }
}
