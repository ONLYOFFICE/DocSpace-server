package com.asc.registration.core.domain.event;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEvent;
import com.asc.registration.core.domain.entity.Scope;
import java.time.ZonedDateTime;

/** Represents a generic event related to a {@link Scope}. */
public class ScopeEvent implements DomainEvent<Scope> {
  private final Audit audit;
  private final Scope scope;
  private final ZonedDateTime eventAt;

  /**
   * Constructs a ScopeEvent instance.
   *
   * @param audit the audit information related to the event
   * @param scope the scope associated with the event
   * @param eventAt the timestamp when the event occurred
   */
  public ScopeEvent(Audit audit, Scope scope, ZonedDateTime eventAt) {
    this.audit = audit;
    this.scope = scope;
    this.eventAt = eventAt;
  }

  /**
   * Returns the audit information related to the event.
   *
   * @return the audit information
   */
  public Audit getAudit() {
    return this.audit;
  }

  /**
   * Returns the scope associated with the event.
   *
   * @return the scope
   */
  public Scope getScope() {
    return this.scope;
  }

  /**
   * Returns the timestamp when the event occurred.
   *
   * @return the event timestamp
   */
  public ZonedDateTime getEventAt() {
    return this.eventAt;
  }
}
