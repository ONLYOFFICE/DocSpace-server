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

import com.asc.common.service.transfer.message.LoginRegisteredEvent;
import java.time.ZonedDateTime;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

public class LoginEventMapperTest {
  private LoginEventMapper loginEventMapper;

  @BeforeEach
  void setUp() {
    loginEventMapper = new LoginEventMapper();
  }

  @Test
  void whenEventIsMappedToEntity_thenLoginEventEntityIsCreated() {
    var now = ZonedDateTime.now();
    var event =
        LoginRegisteredEvent.builder()
            .login("user")
            .active(true)
            .ip("0.0.0.0")
            .browser("Chrome")
            .platform("Windows")
            .date(now)
            .tenantId(1L)
            .userId("user")
            .page("/login")
            .action(1)
            .description("User login")
            .build();

    var entity = loginEventMapper.toEntity(event);

    assertEquals(event.getLogin(), entity.getLogin());
    assertEquals(event.isActive(), entity.isActive());
    assertEquals(event.getIp(), entity.getIp());
    assertEquals(event.getBrowser(), entity.getBrowser());
    assertEquals(event.getPlatform(), entity.getPlatform());
    assertEquals(event.getDate(), entity.getDate());
    assertEquals(event.getTenantId(), entity.getTenantId());
    assertEquals(event.getUserId(), entity.getUserId());
    assertEquals(event.getPage(), entity.getPage());
    assertEquals(event.getAction(), entity.getAction());
    assertEquals(event.getDescription(), entity.getDescription());
  }
}
