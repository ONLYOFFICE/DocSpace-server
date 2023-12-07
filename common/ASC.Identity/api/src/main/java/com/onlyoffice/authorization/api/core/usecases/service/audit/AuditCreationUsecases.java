package com.onlyoffice.authorization.api.core.usecases.service.audit;

import com.onlyoffice.authorization.api.web.server.transfer.messages.AuditMessage;

import java.util.List;

public interface AuditCreationUsecases {
    List<String> saveAudits(Iterable<AuditMessage> audits);
}
