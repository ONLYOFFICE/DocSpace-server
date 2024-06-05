package com.asc.common.service.ports.output.repository;

import com.asc.common.core.domain.entity.Audit;

public interface AuditCommandRepository {
  void saveAudit(Audit audit);
}
