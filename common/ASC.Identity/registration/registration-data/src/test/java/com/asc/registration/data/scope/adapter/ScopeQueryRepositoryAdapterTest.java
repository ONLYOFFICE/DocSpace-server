// (c) Copyright Ascensio System SIA 2009-2025
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
import org.junit.jupiter.api.Test;
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

  @Test
  void whenScopeIsFoundByName_thenReturnScope() {
    when(jpaScopeRepository.findById("testScope")).thenReturn(Optional.of(scopeEntity));

    var result = scopeQueryRepositoryAdapter.findByName("testScope");

    verify(jpaScopeRepository, times(1)).findById("testScope");
    verify(scopeDataAccessMapper, times(1)).toDomain(any(ScopeEntity.class));

    assertTrue(result.isPresent());
    assertEquals("testScope", result.get().getName());
  }

  @Test
  void whenAllScopesAreQueried_thenReturnAllScopes() {
    when(jpaScopeRepository.findAll()).thenReturn(Collections.singletonList(scopeEntity));

    var result = scopeQueryRepositoryAdapter.findAll();

    verify(jpaScopeRepository, times(1)).findAll();
    verify(scopeDataAccessMapper, times(1)).toDomain(any(ScopeEntity.class));

    assertEquals(1, result.size());
    assertEquals("testScope", result.iterator().next().getName());
  }
}
