package com.asc.common.service.ports.output.repository;

import com.asc.common.core.domain.entity.Audit;

/**
 * Interface for the Audit Command Repository.
 *
 * <p>This repository provides methods to manage the persistence of audit records within the
 * application.
 *
 * @see com.asc.common.core.domain.entity.Audit
 */
public interface AuditCommandRepository {

  /**
   * Persists a new audit record.
   *
   * <p>This method is responsible for saving a new audit record to the underlying data store based
   * on the provided {@link Audit} object.
   *
   * @param audit the {@link Audit} object containing the audit details to be saved.
   */
  void saveAudit(Audit audit);
}
