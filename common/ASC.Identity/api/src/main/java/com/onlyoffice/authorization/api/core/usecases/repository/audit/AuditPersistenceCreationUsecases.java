package com.onlyoffice.authorization.api.core.usecases.repository.audit;

import com.onlyoffice.authorization.api.core.entities.Audit;

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