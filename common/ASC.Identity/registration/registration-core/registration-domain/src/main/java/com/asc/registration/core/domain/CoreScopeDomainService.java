package com.asc.registration.core.domain;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.core.domain.entity.Scope;
import com.asc.registration.core.domain.event.ScopeCreatedEvent;
import com.asc.registration.core.domain.event.ScopeDeletedEvent;
import com.asc.registration.core.domain.event.ScopeUpdatedEvent;
import java.time.ZoneId;
import java.time.ZonedDateTime;

/**
 * Implements the {@link ScopeDomainService} interface, providing the core domain logic for managing
 * scopes.
 */
public class CoreScopeDomainService implements ScopeDomainService {
  private static final String UTC = "UTC";

  /**
   * Creates a new scope.
   *
   * @param audit the audit information related to the creation
   * @param scope the scope to be created
   * @return an event indicating the scope was created
   */
  public ScopeCreatedEvent createScope(Audit audit, Scope scope) {
    scope =
        Scope.Builder.builder()
            .name(scope.getName())
            .group(scope.getGroup())
            .type(scope.getType())
            .build();
    return new ScopeCreatedEvent(audit, scope, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Updates the group of an existing scope.
   *
   * @param audit the audit information related to the update
   * @param scope the scope to be updated
   * @param newGroup the new group for the scope
   * @return an event indicating the scope's group was updated
   */
  public ScopeUpdatedEvent updateScopeGroup(Audit audit, Scope scope, String newGroup) {
    scope.updateGroup(newGroup);
    return new ScopeUpdatedEvent(audit, scope, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Updates the type of an existing scope.
   *
   * @param audit the audit information related to the update
   * @param scope the scope to be updated
   * @param newType the new type for the scope
   * @return an event indicating the scope's type was updated
   */
  public ScopeUpdatedEvent updateScopeType(Audit audit, Scope scope, String newType) {
    scope.updateType(newType);
    return new ScopeUpdatedEvent(audit, scope, ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Deletes an existing scope.
   *
   * @param audit the audit information related to the deletion
   * @param scope the scope to be deleted
   * @return an event indicating the scope was deleted
   */
  public ScopeDeletedEvent deleteScope(Audit audit, Scope scope) {
    return new ScopeDeletedEvent(audit, scope, ZonedDateTime.now(ZoneId.of(UTC)));
  }
}
