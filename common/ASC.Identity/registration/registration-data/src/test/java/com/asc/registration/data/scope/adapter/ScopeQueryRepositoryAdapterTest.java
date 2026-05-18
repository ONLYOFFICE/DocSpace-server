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

package com.asc.registration.data.scope.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.*;

import com.asc.registration.core.domain.entity.Scope;
import com.asc.registration.data.scope.entity.ScopeEntity;
import com.asc.registration.data.scope.mapper.ScopeDataAccessMapper;
import com.asc.registration.data.scope.repository.JpaScopeRepository;
import java.util.Collections;
import java.util.Optional;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

class ScopeQueryRepositoryAdapterTest {
  @InjectMocks private ScopeQueryRepositoryAdapter scopeQueryRepositoryAdapter;
  @Mock private JpaScopeRepository jpaScopeRepository;
  @Mock private ScopeDataAccessMapper scopeDataAccessMapper;

  private ScopeEntity scopeEntity;
  private Scope scope;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    scopeEntity = new ScopeEntity();
    scopeEntity.setName("testScope");
    scopeEntity.setGroup("testGroup");
    scopeEntity.setType("testType");

    scope = Scope.Builder.builder().name("testScope").group("testGroup").type("testType").build();

    when(scopeDataAccessMapper.toDomain(any(ScopeEntity.class))).thenReturn(scope);
  }

  private static void assertScopeFieldEquals(String field, Scope actual) {
    switch (field) {
      case "name" -> assertEquals("testScope", actual.getName());
      case "group" -> assertEquals("testGroup", actual.getGroup());
      case "type" -> assertEquals("testType", actual.getType());
      default -> throw new IllegalArgumentException("Unknown field: " + field);
    }
  }

  @ParameterizedTest
  @CsvSource({"name", "group", "type"})
  void whenScopeIsFoundByName_thenScopeFieldMatches(String field) {
    when(jpaScopeRepository.findById("testScope")).thenReturn(Optional.of(scopeEntity));

    var result = scopeQueryRepositoryAdapter.findByName("testScope");

    verify(jpaScopeRepository, times(1)).findById("testScope");
    verify(scopeDataAccessMapper, times(1)).toDomain(any(ScopeEntity.class));

    assertTrue(result.isPresent());
    assertScopeFieldEquals(field, result.get());
  }

  @ParameterizedTest
  @CsvSource({"name", "group", "type"})
  void whenAllScopesAreQueried_thenFirstScopeFieldMatches(String field) {
    when(jpaScopeRepository.findAll()).thenReturn(Collections.singletonList(scopeEntity));

    var result = scopeQueryRepositoryAdapter.findAll();

    verify(jpaScopeRepository, times(1)).findAll();
    verify(scopeDataAccessMapper, times(1)).toDomain(any(ScopeEntity.class));

    assertEquals(1, result.size());
    assertScopeFieldEquals(field, result.iterator().next());
  }
}
