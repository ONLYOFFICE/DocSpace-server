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

package com.asc.authorization.application.mapper;

import static org.junit.jupiter.api.Assertions.assertEquals;

import com.asc.common.service.transfer.message.AuditMessage;
import java.time.ZonedDateTime;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

public class AuditMapperTest {
  private AuditMapper auditMapper;

  @BeforeEach
  void setUp() {
    auditMapper = new AuditMapper();
  }

  @Test
  void whenMessageIsMappedToEntity_thenAuditEntityIsCreated() {
    var now = ZonedDateTime.now();
    var message =
        AuditMessage.builder()
            .initiator("initiator")
            .target("target")
            .ip("ip")
            .browser("browser")
            .platform("platform")
            .date(now)
            .tenantId(1)
            .userId("userId")
            .page("page")
            .action(1)
            .description("description")
            .build();

    var entity = auditMapper.toEntity(message);

    assertEquals(message.getInitiator(), entity.getInitiator());
    assertEquals(message.getTarget(), entity.getTarget());
    assertEquals(message.getIp(), entity.getIp());
    assertEquals(message.getBrowser(), entity.getBrowser());
    assertEquals(message.getPlatform(), entity.getPlatform());
    assertEquals(message.getDate(), entity.getDate());
    assertEquals(message.getTenantId(), entity.getTenantId());
    assertEquals(message.getUserId(), entity.getUserId());
    assertEquals(message.getPage(), entity.getPage());
    assertEquals(message.getAction(), entity.getAction());
    assertEquals(message.getDescription(), entity.getDescription());
  }
}
