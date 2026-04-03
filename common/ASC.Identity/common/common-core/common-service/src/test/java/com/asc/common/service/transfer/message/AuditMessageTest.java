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
