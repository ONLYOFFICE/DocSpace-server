package com.asc.registration.core.domain;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.core.domain.entity.Scope;
import com.asc.registration.core.domain.event.ScopeCreatedEvent;
import com.asc.registration.core.domain.event.ScopeDeletedEvent;
import com.asc.registration.core.domain.event.ScopeUpdatedEvent;

/** Interface for Scope domain service. */
public interface ScopeDomainService {
  /**
   * Creates a new scope.
   *
   * @param audit the audit information related to the creation
   * @param scope the scope to be created
   * @return an event indicating the scope was created
   */
  ScopeCreatedEvent createScope(Audit audit, Scope scope);

  /**
   * Updates the group of an existing scope.
   *
   * @param audit the audit information related to the update
   * @param scope the scope to be updated
   * @param newGroup the new group for the scope
   * @return an event indicating the scope's group was updated
   */
  ScopeUpdatedEvent updateScopeGroup(Audit audit, Scope scope, String newGroup);

  /**
   * Updates the type of an existing scope.
   *
   * @param audit the audit information related to the update
   * @param scope the scope to be updated
   * @param newType the new type for the scope
   * @return an event indicating the scope's type was updated
   */
  ScopeUpdatedEvent updateScopeType(Audit audit, Scope scope, String newType);

  /**
   * Deletes an existing scope.
   *
   * @param audit the audit information related to the deletion
   * @param scope the scope to be deleted
   * @return an event indicating the scope was deleted
   */
  ScopeDeletedEvent deleteScope(Audit audit, Scope scope);
}
