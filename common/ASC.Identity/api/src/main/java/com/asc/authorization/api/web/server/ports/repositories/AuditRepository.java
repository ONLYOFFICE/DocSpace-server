package com.asc.authorization.api.web.server.ports.repositories;

import com.asc.authorization.api.core.entities.Audit;
import com.asc.authorization.api.core.usecases.repository.audit.AuditPersistenceCreationUsecases;
import org.springframework.data.repository.CrudRepository;

/**
 *
 */
public interface AuditRepository extends CrudRepository<Audit, Integer>,
        AuditPersistenceCreationUsecases {
    /**
     *
     * @param entity
     * @return
     */
    default Audit saveAudit(Audit entity) {
        return this.save(entity);
    }
}
