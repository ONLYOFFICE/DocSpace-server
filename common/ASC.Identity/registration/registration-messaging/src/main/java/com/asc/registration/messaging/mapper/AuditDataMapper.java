package com.asc.registration.messaging.mapper;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.transfer.message.AuditMessage;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import org.springframework.stereotype.Component;

/**
 * Mapper class for converting between {@link Audit} and {@link AuditMessage} objects. Provides
 * methods to map from domain entities to messaging data transfer objects and vice versa.
 */
@Component
public class AuditDataMapper {
  private static final String UTC = "UTC";

  /**
   * Converts an {@link Audit} object to an {@link AuditMessage}.
   *
   * @param audit the audit object to be converted
   * @return the resulting audit message
   */
  public AuditMessage toMessage(Audit audit) {
    return AuditMessage.builder()
        .action(audit.getAuditCode().getCode())
        .initiator(audit.getInitiator())
        .target(audit.getTarget())
        .ip(audit.getIp())
        .browser(audit.getBrowser())
        .platform(audit.getPlatform())
        .tenantId(audit.getTenantId())
        .userEmail(audit.getUserEmail())
        .userName(audit.getUserName())
        .userId(audit.getUserId())
        .page(audit.getPage())
        .description(audit.getDescription())
        .date(ZonedDateTime.now(ZoneId.of(UTC)))
        .build();
  }

  /**
   * Converts an {@link AuditMessage} object to an {@link Audit}.
   *
   * @param message the audit message to be converted
   * @return the resulting audit object
   */
  public Audit toAudit(AuditMessage message) {
    return Audit.Builder.builder()
        .auditCode(AuditCode.of(message.getAction()))
        .initiator(message.getInitiator())
        .target(message.getTarget())
        .ip(message.getIp())
        .browser(message.getBrowser())
        .platform(message.getPlatform())
        .tenantId(message.getTenantId())
        .userEmail(message.getUserEmail())
        .userName(message.getUserName())
        .userId(message.getUserId())
        .page(message.getPage())
        .description(message.getDescription())
        .build();
  }
}
