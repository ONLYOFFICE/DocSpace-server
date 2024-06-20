package com.asc.common.service.ports.input.service;

import com.asc.common.core.domain.entity.Audit;

/**
 * Interface for the Audit Application Service.
 *
 * <p>This service provides methods to manage audit records within the application.
 *
 * @see com.asc.common.core.domain.entity.Audit
 */
public interface AuditApplicationService {

  /**
   * Creates a new audit record.
   *
   * <p>This method is responsible for creating and persisting a new audit record based on the
   * provided {@link Audit} object.
   *
   * @param audit the {@link Audit} object containing the audit details to be created.
   * @return the created {@link Audit} object with any generated fields populated (e.g., ID,
   *     timestamps).
   */
  Audit createAudit(Audit audit);
}
