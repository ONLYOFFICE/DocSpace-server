package com.onlyoffice.authorization.api.core.usecases.service.audit;

import com.onlyoffice.authorization.api.web.server.messaging.messages.AuditMessage;

import java.util.Set;

public interface AuditCreationUsecases {
    Set<String> saveAudits(Iterable<AuditMessage> audits);
}
