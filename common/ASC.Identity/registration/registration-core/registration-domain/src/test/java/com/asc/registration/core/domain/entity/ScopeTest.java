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

package com.asc.registration.core.domain.entity;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.registration.core.domain.exception.ScopeDomainException;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;

class ScopeTest {
  private Scope scope;

  @BeforeEach
  void setUp() {
    scope = Scope.Builder.builder().name("read").group("user").type("permission").build();
  }

  @Test
  void whenScopeIsCreated_thenFieldsAreSetCorrectly() {
    assertNotNull(scope);
    assertEquals("read", scope.getName());
    assertEquals("user", scope.getGroup());
    assertEquals("permission", scope.getType());
  }

  @ParameterizedTest
  @CsvSource({"admin,group", "guest,group", "role,type", "revoked,type"})
  void whenScopePropertyIsUpdated_thenValueIsUpdatedCorrectly(String value, String property) {
    if ("group".equals(property)) {
      scope.updateGroup(value);
      assertEquals(value, scope.getGroup());
      return;
    }

    scope.updateType(value);
    assertEquals(value, scope.getType());
  }

  @ParameterizedTest
  @CsvSource({
    "'',user,permission,Scope name must not be null or empty",
    "read,'',permission,Scope group must not be null or empty",
    "read,user,'',Scope type must not be null or empty"
  })
  void whenScopeIsInvalid_thenValidationThrowsException(
      String name, String group, String type, String expectedMessage) {
    var exception =
        assertThrows(
            ScopeDomainException.class,
            () -> Scope.Builder.builder().name(name).group(group).type(type).build());

    assertEquals(expectedMessage, exception.getMessage());
  }
}
