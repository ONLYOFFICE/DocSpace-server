package com.asc.common.service.ports.input.service;

import com.asc.common.core.domain.entity.Audit;

public interface AuditApplicationService {
  Audit createAudit(Audit audit);
}
