package com.asc.common.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.service.ports.output.repository.AuditCommandRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

@Slf4j
@Component
@RequiredArgsConstructor
public class AuditCreateCommandHandler {
  private final AuditCommandRepository auditCommandRepository;

  @Transactional(timeout = 2)
  public void createAudit(Audit audit) {
    auditCommandRepository.saveAudit(audit);
  }

  @Transactional(timeout = 4)
  public void createAudits(Iterable<Audit> audits) {
    for (var audit : audits) auditCommandRepository.saveAudit(audit);
  }
}
