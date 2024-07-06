// (c) Copyright Ascensio System SIA 2009-2024
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
