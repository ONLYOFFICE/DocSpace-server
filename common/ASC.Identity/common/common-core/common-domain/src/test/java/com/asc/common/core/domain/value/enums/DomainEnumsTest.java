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

package com.asc.common.core.domain.value.enums;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;

class DomainEnumsTest {
  @Test
  void whenAuditCodeGetCode_thenReturnsExpectedInteger() {
    assertEquals(9901, AuditCode.CREATE_CLIENT.getCode());
    assertEquals(9902, AuditCode.UPDATE_CLIENT.getCode());
    assertEquals(9903, AuditCode.REGENERATE_SECRET.getCode());
    assertEquals(9904, AuditCode.DELETE_CLIENT.getCode());
  }

  @Test
  void whenAuditCodeOf_thenReturnsExpectedEnum() {
    assertEquals(AuditCode.CREATE_CLIENT, AuditCode.of(9901));
    assertEquals(AuditCode.GENERATE_PERSONAL_ACCESS_TOKEN, AuditCode.of(9909));
  }

  @Test
  void whenAuditCodeOfInvalid_thenThrowsIllegalArgumentException() {
    assertThrows(IllegalArgumentException.class, () -> AuditCode.of(-1));
  }

  @Test
  void whenAuthenticationMethodFromMethod_thenReturnsExpectedEnum() {
    assertEquals(
        AuthenticationMethod.DEFAULT_AUTHENTICATION,
        AuthenticationMethod.fromMethod("client_secret_post"));
    assertEquals(AuthenticationMethod.PKCE_AUTHENTICATION, AuthenticationMethod.fromMethod("none"));
  }

  @Test
  void whenAuthenticationMethodFromMethodInvalid_thenThrowsIllegalArgumentException() {
    var exception =
        assertThrows(
            IllegalArgumentException.class,
            () -> AuthenticationMethod.fromMethod("invalid-method"));

    assertEquals("No enum constant for method: invalid-method", exception.getMessage());
  }

  @ParameterizedTest
  @CsvSource({
    "AUTHORIZATION_CODE,authorization_code",
    "REFRESH_TOKEN,refresh_token",
    "CLIENT_CREDENTIALS,client_credentials"
  })
  void whenGrantTypeGetType_thenReturnsExpectedString(GrantType grantType, String expectedType) {
    assertEquals(expectedType, grantType.getType());
  }
}
