package com.asc.common.data.audit.adapter;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.data.audit.mapper.AuditDataAccessMapper;
import com.asc.common.data.audit.repository.JpaAuditRepository;
import com.asc.common.service.ports.output.repository.AuditCommandRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Repository;

/**
 * Adapter class that implements the {@link AuditCommandRepository} interface and provides the
 * implementation for saving audit records to the database using JPA.
 */
@Repository
@RequiredArgsConstructor
public class AuditCommandRepositoryDomainAdapter implements AuditCommandRepository {
  private final JpaAuditRepository jpaAuditRepository;
  private final AuditDataAccessMapper auditDataAccessMapper;

  /**
   * Saves an audit record to the database.
   *
   * @param audit the audit record to be saved
   */
  public void saveAudit(Audit audit) {
    jpaAuditRepository.save(auditDataAccessMapper.toEntity(audit));
  }
}
