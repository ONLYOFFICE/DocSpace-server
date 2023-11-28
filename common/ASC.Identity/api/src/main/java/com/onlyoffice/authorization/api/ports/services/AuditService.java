package com.onlyoffice.authorization.api.ports.services;

import com.onlyoffice.authorization.api.core.transfer.messages.AuditMessage;
import com.onlyoffice.authorization.api.core.usecases.repository.audit.AuditPersistenceMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.audit.AuditCreationUsecases;
import com.onlyoffice.authorization.api.external.mappers.AuditMapper;
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
                MDC.put("audit_user_id", audit.getUserId());
                MDC.put("audit_ip", audit.getIp());
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
