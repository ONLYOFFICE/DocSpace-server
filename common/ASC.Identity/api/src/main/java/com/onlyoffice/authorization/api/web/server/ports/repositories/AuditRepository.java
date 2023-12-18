package com.onlyoffice.authorization.api.web.server.ports.repositories;

import com.onlyoffice.authorization.api.core.entities.Audit;
import com.onlyoffice.authorization.api.core.usecases.repository.audit.AuditPersistenceCreationUsecases;
import org.springframework.data.repository.CrudRepository;

public interface AuditRepository extends CrudRepository<Audit, Integer>,
        AuditPersistenceCreationUsecases {
    default Audit saveAudit(Audit entity) {
        return this.save(entity);
    }
}
