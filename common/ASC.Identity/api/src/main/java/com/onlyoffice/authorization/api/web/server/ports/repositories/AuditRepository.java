package com.onlyoffice.authorization.api.web.server.ports.repositories;

import com.onlyoffice.authorization.api.core.entities.Audit;
import com.onlyoffice.authorization.api.core.usecases.repository.audit.AuditPersistenceMutationUsecases;
import org.springframework.data.repository.CrudRepository;

public interface AuditRepository extends CrudRepository<Audit, Integer>,
        AuditPersistenceMutationUsecases {
    default Audit saveAudit(Audit entity) {
        return this.save(entity);
    }
}
