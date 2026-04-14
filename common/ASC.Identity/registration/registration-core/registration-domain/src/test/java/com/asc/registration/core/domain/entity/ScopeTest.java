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
