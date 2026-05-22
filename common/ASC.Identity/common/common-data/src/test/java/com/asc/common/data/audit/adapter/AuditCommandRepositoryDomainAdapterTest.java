// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
  void givenAudit_whenSaving_thenRepositorySavesAuditEntityCorrectly() {
    when(auditDataAccessMapper.toEntity(audit)).thenReturn(auditEntity);

    auditCommandRepositoryDomainAdapter.saveAudit(audit);

    var auditEntityArgumentCaptor = ArgumentCaptor.forClass(AuditEntity.class);

    verify(jpaAuditRepository).save(auditEntityArgumentCaptor.capture());
    assertEquals(auditEntity, auditEntityArgumentCaptor.getValue());
  }
}
