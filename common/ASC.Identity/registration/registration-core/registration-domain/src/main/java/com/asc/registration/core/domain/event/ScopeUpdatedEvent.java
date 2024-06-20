package com.asc.registration.core.domain.event;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.core.domain.entity.Scope;
import java.time.ZonedDateTime;

/** Represents an event indicating that a {@link Scope} has been updated. */
public class ScopeUpdatedEvent extends ScopeEvent {
  /**
   * Constructs a ScopeUpdatedEvent instance.
   *
   * @param audit the audit information related to the event
   * @param scope the scope that has been created
   * @param eventAt the timestamp when the event occurred
   */
  public ScopeUpdatedEvent(Audit audit, Scope scope, ZonedDateTime eventAt) {
    super(audit, scope, eventAt);
  }
}
