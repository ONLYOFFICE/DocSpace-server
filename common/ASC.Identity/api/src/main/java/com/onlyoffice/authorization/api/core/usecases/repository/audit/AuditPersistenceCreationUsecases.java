package com.onlyoffice.authorization.api.core.usecases.repository.audit;

import com.onlyoffice.authorization.api.core.entities.Audit;

public interface AuditPersistenceCreationUsecases {
    Audit saveAudit(Audit entity);
}