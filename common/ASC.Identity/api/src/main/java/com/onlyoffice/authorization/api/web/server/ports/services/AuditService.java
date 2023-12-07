package com.onlyoffice.authorization.api.web.server.ports.services;

import com.onlyoffice.authorization.api.web.server.transfer.messages.AuditMessage;
import com.onlyoffice.authorization.api.core.usecases.repository.audit.AuditPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.audit.AuditCreationUsecases;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.AuditMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

@Slf4j
@Component
@RequiredArgsConstructor
public class AuditService implements AuditCreationUsecases {
    private final AuditPersistenceMutationUsecases auditUsecases;
    @Transactional
    public List<String> saveAudits(Iterable<AuditMessage> audits) {
        List<String> ids = new ArrayList<>();
        for (AuditMessage audit : audits) {
            try {
                MDC.put("auditUserId", audit.getUserId());
                MDC.put("auditIp", audit.getIp());
                log.info("Saving an audit");
                auditUsecases.saveAudit(AuditMapper.INSTANCE.toEntity(audit));
            } catch (Exception e) {
                ids.add(audit.getTag());
                log.error("Could not persist an authorization", e);
            } finally {
                MDC.clear();
            }
        }
        return ids;
    }
}
