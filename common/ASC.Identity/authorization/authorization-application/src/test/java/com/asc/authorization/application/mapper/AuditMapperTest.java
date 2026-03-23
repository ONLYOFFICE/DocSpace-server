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
