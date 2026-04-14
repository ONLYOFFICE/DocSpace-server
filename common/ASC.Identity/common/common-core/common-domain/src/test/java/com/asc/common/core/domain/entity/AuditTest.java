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

package com.asc.common.core.domain.entity;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;

import com.asc.common.core.domain.exception.AuditDomainException;
import com.asc.common.core.domain.value.enums.AuditCode;
import java.util.function.Consumer;
import java.util.stream.Stream;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

class AuditTest {
  private static final String VALID_IP = "127.0.0.1";
  private static final String VALID_BROWSER = "chrome";
  private static final String VALID_PLATFORM = "macos";
  private static final long VALID_TENANT_ID = 1L;
  private static final String VALID_EMAIL = "test@example.com";
  private static final String VALID_NAME = "Test User";
  private static final String VALID_USER_ID = "user-1";
  private static final String VALID_PAGE = "/page";
  private static final AuditCode VALID_AUDIT_CODE = AuditCode.CREATE_CLIENT;

  private static Audit.Builder validBuilder() {
    return Audit.Builder.builder()
        .auditCode(VALID_AUDIT_CODE)
        .ip(VALID_IP)
        .browser(VALID_BROWSER)
        .platform(VALID_PLATFORM)
        .tenantId(VALID_TENANT_ID)
        .userEmail(VALID_EMAIL)
        .userName(VALID_NAME)
        .userId(VALID_USER_ID)
        .page(VALID_PAGE);
  }

  static Stream<Arguments> invalidAuditCases() {
    return java.util.stream.Stream.of(
        Arguments.of((Consumer<Audit.Builder>) builder -> builder.ip(""), "Sender must have ip"),
        Arguments.of(
            (Consumer<Audit.Builder>) builder -> builder.browser(""),
            "Sender must have browser or user agent"),
        Arguments.of(
            (Consumer<Audit.Builder>) builder -> builder.platform(""), "Sender must have platform"),
        Arguments.of(
            (Consumer<Audit.Builder>) builder -> builder.tenantId(0L),
            "Sender must have a valid tenant id"),
        Arguments.of(
            (Consumer<Audit.Builder>) builder -> builder.userEmail(""),
            "Sender must have a valid email"),
        Arguments.of(
            (Consumer<Audit.Builder>) builder -> builder.userName(""),
            "Sender must have a valid name"),
        Arguments.of(
            (Consumer<Audit.Builder>) builder -> builder.userId(""),
            "Sender must have a valid user id"),
        Arguments.of(
            (Consumer<Audit.Builder>) builder -> builder.page(""), "Sender must have a valid page"),
        Arguments.of(
            (Consumer<Audit.Builder>) builder -> builder.auditCode(null),
            "Sender must provide audit code"));
  }

  @Test
  void whenAuditIsBuilt_thenFieldsAreSetCorrectly() {
    var audit = validBuilder().build();

    assertNotNull(audit);
    assertEquals(0L, audit.getId());
    assertEquals(VALID_AUDIT_CODE, audit.getAuditCode());
    assertEquals(VALID_IP, audit.getIp());
    assertEquals(VALID_BROWSER, audit.getBrowser());
    assertEquals(VALID_PLATFORM, audit.getPlatform());
    assertEquals(VALID_TENANT_ID, audit.getTenantId());
    assertEquals(VALID_EMAIL, audit.getUserEmail());
    assertEquals(VALID_NAME, audit.getUserName());
    assertEquals(VALID_USER_ID, audit.getUserId());
    assertEquals(VALID_PAGE, audit.getPage());
  }

  @ParameterizedTest
  @MethodSource("invalidAuditCases")
  void whenAuditIsInvalid_thenValidationThrowsException(
      Consumer<Audit.Builder> mutator, String expectedMessage) {
    var builder = validBuilder();
    mutator.accept(builder);

    var exception = assertThrows(AuditDomainException.class, builder::build);

    assertEquals(expectedMessage, exception.getMessage());
  }
}
