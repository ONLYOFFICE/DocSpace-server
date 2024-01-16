package com.asc.authorization.api.core.usecases.repository.audit;

import com.asc.authorization.api.core.entities.Audit;

/**
 *
 */
public interface AuditPersistenceCreationUsecases {
    /**
     *
     * @param entity
     * @return
     */
    Audit saveAudit(Audit entity);
}