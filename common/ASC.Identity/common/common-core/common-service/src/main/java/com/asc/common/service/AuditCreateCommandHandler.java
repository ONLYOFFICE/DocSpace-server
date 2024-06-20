package com.asc.common.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.service.ports.output.repository.AuditCommandRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * Handles the creation of audit records by interacting with the {@link AuditCommandRepository}.
 *
 * <p>This class is responsible for saving single or multiple audit records in the repository. It
 * uses Spring's {@link Transactional} annotation to manage transactions and ensures that the
 * operations are completed within specified timeouts.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AuditCreateCommandHandler {

  private final AuditCommandRepository auditCommandRepository;

  /**
   * Creates a single audit record.
   *
   * <p>This method saves a single audit record in the repository. The transaction timeout for this
   * operation is set to 2 seconds.
   *
   * @param audit the audit record to be saved
   */
  @Transactional(timeout = 2)
  public void createAudit(Audit audit) {
    auditCommandRepository.saveAudit(audit);
  }

  /**
   * Creates multiple audit records.
   *
   * <p>This method saves multiple audit records in the repository. The transaction timeout for this
   * operation is set to 4 seconds.
   *
   * @param audits the iterable collection of audit records to be saved
   */
  @Transactional(timeout = 4)
  public void createAudits(Iterable<Audit> audits) {
    for (var audit : audits) auditCommandRepository.saveAudit(audit);
  }
}
