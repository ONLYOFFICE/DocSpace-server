// (c) Copyright Ascensio System SIA 2009-2026
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
