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

package com.asc.common.service.transfer.message;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

import java.time.ZonedDateTime;
import org.junit.jupiter.api.Test;

class AuditMessageTest {
  @Test
  void givenAuditMessageInputs_whenBuildingAuditMessage_thenFieldsAreAssigned() {
    var date = ZonedDateTime.now();

    var message =
        AuditMessage.builder()
            .tag("tag")
            .initiator("initiator")
            .target("target")
            .ip("127.0.0.1")
            .browser("chrome")
            .platform("macos")
            .date(date)
            .tenantId(1L)
            .userEmail("test@example.com")
            .userName("Test User")
            .userId("user-1")
            .page("/page")
            .action(10)
            .description("desc")
            .build();

    assertNotNull(message);
    assertEquals("tag", message.getTag());
    assertEquals("initiator", message.getInitiator());
    assertEquals("target", message.getTarget());
    assertEquals(1L, message.getTenantId());
    assertEquals("/page", message.getPage());
    assertEquals(10, message.getAction());
    assertEquals(date, message.getDate());
  }

  @Test
  void givenTwoMessagesWithSameFields_whenComparing_thenTheyAreEqual() {
    var date = ZonedDateTime.now();

    var messageOne =
        AuditMessage.builder()
            .tag("tag")
            .initiator("initiator")
            .target("target")
            .ip("127.0.0.1")
            .browser("chrome")
            .platform("macos")
            .date(date)
            .tenantId(1L)
            .userEmail("test@example.com")
            .userName("Test User")
            .userId("user-1")
            .page("/page")
            .action(10)
            .description("desc")
            .build();

    var messageTwo =
        AuditMessage.builder()
            .tag("tag")
            .initiator("initiator")
            .target("target")
            .ip("127.0.0.1")
            .browser("chrome")
            .platform("macos")
            .date(date)
            .tenantId(1L)
            .userEmail("test@example.com")
            .userName("Test User")
            .userId("user-1")
            .page("/page")
            .action(10)
            .description("desc")
            .build();

    assertEquals(messageOne, messageTwo);
    assertEquals(messageOne.hashCode(), messageTwo.hashCode());
  }
}
