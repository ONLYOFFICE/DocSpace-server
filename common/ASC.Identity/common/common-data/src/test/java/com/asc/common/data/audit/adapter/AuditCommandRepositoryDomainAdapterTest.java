package com.asc.common.data.audit.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.data.audit.entity.AuditEntity;
import com.asc.common.data.audit.mapper.AuditDataAccessMapper;
import com.asc.common.data.audit.repository.JpaAuditRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class AuditCommandRepositoryDomainAdapterTest {
  @Mock private JpaAuditRepository jpaAuditRepository;
  @Mock private AuditDataAccessMapper auditDataAccessMapper;
  @InjectMocks private AuditCommandRepositoryDomainAdapter auditCommandRepositoryDomainAdapter;

  private Audit audit;
  private AuditEntity auditEntity;

  @BeforeEach
  void setUp() {
    audit =
        Audit.Builder.builder()
            .auditCode(AuditCode.CREATE_CLIENT)
            .initiator("initiator")
            .target("target")
            .ip("ip")
            .browser("browser")
            .platform("platform")
            .tenantId(1)
            .userEmail("email")
            .userName("name")
            .userId("id")
            .page("page")
            .description("description")
            .build();
    auditEntity =
        AuditEntity.builder()
            .action(AuditCode.CREATE_CLIENT.getCode())
            .initiator("initiator")
            .target("target")
            .ip("ip")
            .browser("browser")
            .platform("platform")
            .tenantId(1)
            .userId("id")
            .page("page")
            .description("description")
            .build();
  }

  @Test
  void saveAudit() {
    // Arrange
    when(auditDataAccessMapper.toEntity(audit)).thenReturn(auditEntity);

    // Act
    auditCommandRepositoryDomainAdapter.saveAudit(audit);

    // Assert
    ArgumentCaptor<AuditEntity> auditEntityArgumentCaptor =
        ArgumentCaptor.forClass(AuditEntity.class);
    verify(jpaAuditRepository).save(auditEntityArgumentCaptor.capture());
    assertEquals(auditEntity, auditEntityArgumentCaptor.getValue());
  }
}
