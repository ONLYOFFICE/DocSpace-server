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

package com.asc.common.data.audit.mapper;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertThrows;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.data.audit.entity.AuditEntity;
import java.util.stream.Stream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

class AuditDataAccessMapperTest {
  private AuditDataAccessMapper mapper;

  @BeforeEach
  void setUp() {
    mapper = new AuditDataAccessMapper();
  }

  private static Audit createValidAudit() {
    return Audit.Builder.builder()
        .auditCode(AuditCode.CREATE_CLIENT)
        .initiator("initiator")
        .target("target")
        .ip("ip")
        .browser("browser")
        .platform("platform")
        .tenantId(1L)
        .userEmail("email")
        .userName("name")
        .userId("id")
        .page("page")
        .description("description")
        .build();
  }

  static Stream<Arguments> mappingCases() {
    var audit = createValidAudit();
    var expectedEntity =
        AuditEntity.builder()
            .action(audit.getAuditCode().getCode())
            .initiator(audit.getInitiator())
            .target(audit.getTarget())
            .ip(audit.getIp())
            .browser(audit.getBrowser())
            .platform(audit.getPlatform())
            .tenantId(audit.getTenantId())
            .userId(audit.getUserId())
            .page(audit.getPage())
            .description(audit.getDescription())
            .build();

    return Stream.of(
        Arguments.of("action", expectedEntity.getAction()),
        Arguments.of("initiator", expectedEntity.getInitiator()),
        Arguments.of("target", expectedEntity.getTarget()),
        Arguments.of("ip", expectedEntity.getIp()),
        Arguments.of("browser", expectedEntity.getBrowser()),
        Arguments.of("platform", expectedEntity.getPlatform()),
        Arguments.of("tenantId", expectedEntity.getTenantId()),
        Arguments.of("userId", expectedEntity.getUserId()),
        Arguments.of("page", expectedEntity.getPage()),
        Arguments.of("description", expectedEntity.getDescription()));
  }

  @ParameterizedTest
  @MethodSource("mappingCases")
  void givenAudit_whenMappingToEntity_thenExpectedFieldsMatch(
      String fieldName, Object expectedValue) {
    var audit = createValidAudit();
    var entity = mapper.toEntity(audit);

    switch (fieldName) {
      case "action" ->
          assertEquals(expectedValue, entity.getAction(), "Mismatch on mapped field: " + fieldName);
      case "initiator" ->
          assertEquals(
              expectedValue, entity.getInitiator(), "Mismatch on mapped field: " + fieldName);
      case "target" ->
          assertEquals(expectedValue, entity.getTarget(), "Mismatch on mapped field: " + fieldName);
      case "ip" ->
          assertEquals(expectedValue, entity.getIp(), "Mismatch on mapped field: " + fieldName);
      case "browser" ->
          assertEquals(
              expectedValue, entity.getBrowser(), "Mismatch on mapped field: " + fieldName);
      case "platform" ->
          assertEquals(
              expectedValue, entity.getPlatform(), "Mismatch on mapped field: " + fieldName);
      case "tenantId" ->
          assertEquals(
              expectedValue, entity.getTenantId(), "Mismatch on mapped field: " + fieldName);
      case "userId" ->
          assertEquals(expectedValue, entity.getUserId(), "Mismatch on mapped field: " + fieldName);
      case "page" ->
          assertEquals(expectedValue, entity.getPage(), "Mismatch on mapped field: " + fieldName);
      case "description" ->
          assertEquals(
              expectedValue, entity.getDescription(), "Mismatch on mapped field: " + fieldName);
      default -> throw new IllegalArgumentException("Unknown field: " + fieldName);
    }
    assertNull(entity.getDate(), "Mapper should not set date; it is filled on @PrePersist");
  }

  @Test
  void givenNullAudit_whenMappingToEntity_thenThrowsNullPointerException() {
    assertThrows(NullPointerException.class, () -> mapper.toEntity(null));
  }
}
