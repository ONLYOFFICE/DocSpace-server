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
