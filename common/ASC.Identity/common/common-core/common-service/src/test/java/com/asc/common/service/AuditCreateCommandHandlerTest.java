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

package com.asc.common.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoMoreInteractions;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.ports.output.repository.AuditCommandRepository;
import java.util.List;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

class AuditCreateCommandHandlerTest {
  @InjectMocks private AuditCreateCommandHandler handler;
  @Mock private AuditCommandRepository auditCommandRepository;

  private static final String VALID_IP = "127.0.0.1";
  private static final String VALID_BROWSER = "chrome";
  private static final String VALID_PLATFORM = "macos";
  private static final long VALID_TENANT_ID = 1L;
  private static final String VALID_EMAIL = "test@example.com";
  private static final String VALID_NAME = "Test User";
  private static final String VALID_USER_ID = "user-1";
  private static final String VALID_PAGE = "/page";

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);
  }

  private static Audit createValidAudit(AuditCode code) {
    return Audit.Builder.builder()
        .auditCode(code)
        .ip(VALID_IP)
        .browser(VALID_BROWSER)
        .platform(VALID_PLATFORM)
        .tenantId(VALID_TENANT_ID)
        .userEmail(VALID_EMAIL)
        .userName(VALID_NAME)
        .userId(VALID_USER_ID)
        .page(VALID_PAGE)
        .description("desc")
        .build();
  }

  @Test
  void givenAudit_whenCreateAudit_thenDelegatesToRepository() {
    var audit = createValidAudit(AuditCode.CREATE_CLIENT);

    handler.createAudit(audit);

    verify(auditCommandRepository, times(1)).saveAudit(audit);
  }

  @Test
  void givenAudits_whenCreateAudits_thenDelegatesForEachAudit() {
    var auditOne = createValidAudit(AuditCode.UPDATE_CLIENT);
    var auditTwo = createValidAudit(AuditCode.DELETE_CLIENT);

    handler.createAudits(List.of(auditOne, auditTwo));

    var captor = ArgumentCaptor.forClass(Audit.class);
    verify(auditCommandRepository, times(2)).saveAudit(captor.capture());

    var capturedCodes = captor.getAllValues().stream().map(Audit::getAuditCode).toList();

    assertEquals(List.of(AuditCode.UPDATE_CLIENT, AuditCode.DELETE_CLIENT), capturedCodes);
    verifyNoMoreInteractions(auditCommandRepository);
  }
}
