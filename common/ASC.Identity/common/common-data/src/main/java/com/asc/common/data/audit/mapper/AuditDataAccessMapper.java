package com.asc.common.data.audit.mapper;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.data.audit.entity.AuditEntity;
import org.springframework.stereotype.Component;

/**
 * Mapper class responsible for converting between {@link Audit} domain objects and {@link
 * AuditEntity} data entities.
 */
@Component
public class AuditDataAccessMapper {

  /**
   * Converts a domain {@link Audit} object to a data {@link AuditEntity} object.
   *
   * @param audit the domain audit object to be converted
   * @return the corresponding data entity object
   */
  public AuditEntity toEntity(Audit audit) {
    return AuditEntity.builder()
        .action(audit.getAuditCode().getCode())
        .initiator(audit.getInitiator())
        .target(audit.getTarget())
        .ip(audit.getIp())
        .browser(audit.getBrowser())
        .platform(audit.getPlatform())
        .tenantId(audit.getTenantId())
        .userId(audit.getUserId())
        .page(audit.getPage())
        .description(audit.getDescription())
        .build();
  }
}
