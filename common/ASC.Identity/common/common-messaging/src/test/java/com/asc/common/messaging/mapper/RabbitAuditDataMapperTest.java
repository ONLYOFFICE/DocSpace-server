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

package com.asc.common.messaging.mapper;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.transfer.message.AuditMessage;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import org.junit.jupiter.api.Test;

class RabbitAuditDataMapperTest {
  private static final String VALID_IP = "127.0.0.1";
  private static final String VALID_BROWSER = "chrome";
  private static final String VALID_PLATFORM = "macos";
  private static final long VALID_TENANT_ID = 1L;
  private static final String VALID_EMAIL = "test@example.com";
  private static final String VALID_NAME = "Test User";
  private static final String VALID_USER_ID = "user-1";
  private static final String VALID_PAGE = "/page";

  private final RabbitAuditDataMapper mapper = new RabbitAuditDataMapper();

  @Test
  void whenMappingAuditToMessage_thenFieldsAreAssigned() {
    var audit =
        Audit.Builder.builder()
            .auditCode(AuditCode.UPDATE_CLIENT)
            .initiator("initiator")
            .target("target")
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

    var message = mapper.toMessage(audit);

    assertNotNull(message);
    assertEquals(audit.getAuditCode().getCode(), message.getAction());
    assertEquals(audit.getInitiator(), message.getInitiator());
    assertEquals(audit.getTarget(), message.getTarget());
    assertEquals(audit.getIp(), message.getIp());
    assertEquals(audit.getBrowser(), message.getBrowser());
    assertEquals(audit.getPlatform(), message.getPlatform());
    assertEquals(audit.getTenantId(), message.getTenantId());
    assertEquals(audit.getUserEmail(), message.getUserEmail());
    assertEquals(audit.getUserName(), message.getUserName());
    assertEquals(audit.getUserId(), message.getUserId());
    assertEquals(audit.getPage(), message.getPage());
    assertEquals(audit.getDescription(), message.getDescription());

    assertNotNull(message.getDate());
    assertEquals(ZoneId.of("UTC"), message.getDate().getZone());
  }

  @Test
  void whenMappingMessageToAudit_thenRoundTripAuditFieldsAreAssigned() {
    var date = ZonedDateTime.now(ZoneId.of("UTC"));
    var message =
        AuditMessage.builder()
            .action(AuditCode.UPDATE_CLIENT.getCode())
            .initiator("initiator")
            .target("target")
            .ip(VALID_IP)
            .browser(VALID_BROWSER)
            .platform(VALID_PLATFORM)
            .tenantId(VALID_TENANT_ID)
            .userEmail(VALID_EMAIL)
            .userName(VALID_NAME)
            .userId(VALID_USER_ID)
            .page(VALID_PAGE)
            .description("desc")
            .date(date)
            .build();

    var audit = mapper.toAudit(message);

    assertNotNull(audit);
    assertEquals(AuditCode.UPDATE_CLIENT, audit.getAuditCode());
    assertEquals(message.getInitiator(), audit.getInitiator());
    assertEquals(message.getTarget(), audit.getTarget());
    assertEquals(message.getIp(), audit.getIp());
    assertEquals(message.getBrowser(), audit.getBrowser());
    assertEquals(message.getPlatform(), audit.getPlatform());
    assertEquals(message.getTenantId(), audit.getTenantId());
    assertEquals(message.getUserEmail(), audit.getUserEmail());
    assertEquals(message.getUserName(), audit.getUserName());
    assertEquals(message.getUserId(), audit.getUserId());
    assertEquals(message.getPage(), audit.getPage());
    assertEquals(message.getDescription(), audit.getDescription());
  }

  @Test
  void whenMappingMessageWithUnknownAction_thenThrowsIllegalArgumentException() {
    var message =
        AuditMessage.builder()
            .action(123456)
            .initiator("initiator")
            .target("target")
            .ip(VALID_IP)
            .browser(VALID_BROWSER)
            .platform(VALID_PLATFORM)
            .tenantId(VALID_TENANT_ID)
            .userEmail(VALID_EMAIL)
            .userName(VALID_NAME)
            .userId(VALID_USER_ID)
            .page(VALID_PAGE)
            .description("desc")
            .date(ZonedDateTime.now(ZoneId.of("UTC")))
            .build();

    assertThrows(IllegalArgumentException.class, () -> mapper.toAudit(message));
  }
}
