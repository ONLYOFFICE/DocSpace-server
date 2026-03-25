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

package com.asc.registration.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.*;

import com.asc.registration.core.domain.entity.Scope;
import com.asc.registration.core.domain.exception.ScopeNotFoundException;
import com.asc.registration.service.mapper.ScopeDataMapper;
import com.asc.registration.service.ports.output.repository.ScopeQueryRepository;
import com.asc.registration.service.transfer.response.ScopeResponse;
import java.util.Collections;
import java.util.Optional;
import java.util.function.Function;
import java.util.stream.Stream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

public class ScopeQueryHandlerTest {
  @InjectMocks private ScopeQueryHandler scopeQueryHandler;
  @Mock private ScopeQueryRepository queryRepository;
  @Mock private ScopeDataMapper dataMapper;

  private Scope scope;
  private ScopeResponse scopeResponse;

  @BeforeEach
  public void setUp() {
    MockitoAnnotations.openMocks(this);

    scope =
        Scope.Builder.builder().name("test-scope").group("test-group").type("test-type").build();
    scopeResponse =
        ScopeResponse.builder().name("test-scope").group("test-group").type("test-type").build();
  }

  @ParameterizedTest
  @MethodSource("scopeFoundCases")
  public void whenScopeIsFound_thenReturnScopeResponse(
      String scopeName,
      Function<ScopeResponse, String> expectedExtractor,
      Function<ScopeResponse, String> actualExtractor) {
    when(queryRepository.findByName(anyString())).thenReturn(Optional.of(scope));
    when(dataMapper.toScopeResponse(any(Scope.class))).thenReturn(scopeResponse);

    var response = scopeQueryHandler.getScope(scopeName);

    verify(queryRepository, times(1)).findByName(scopeName);
    verify(dataMapper, times(1)).toScopeResponse(any(Scope.class));

    assertEquals(expectedExtractor.apply(scopeResponse), actualExtractor.apply(response));
  }

  @Test
  public void whenScopeIsNotFound_thenThrowScopeNotFoundException() {
    when(queryRepository.findByName(anyString())).thenReturn(Optional.empty());

    assertThrows(ScopeNotFoundException.class, () -> scopeQueryHandler.getScope("test-scope"));

    verify(queryRepository, times(1)).findByName("test-scope");
    verify(dataMapper, times(0)).toScopeResponse(any(Scope.class));
  }

  @Test
  public void whenScopesAreQueried_thenReturnScopeResponseList() {
    when(queryRepository.findAll()).thenReturn(Collections.singleton(scope));
    when(dataMapper.toScopeResponse(any(Scope.class))).thenReturn(scopeResponse);

    var response = scopeQueryHandler.getScopes();

    verify(queryRepository, times(1)).findAll();
    verify(dataMapper, times(1)).toScopeResponse(any(Scope.class));

    assertEquals(1, response.size());
    var firstResponse = response.iterator().next();
    scopeFieldExtractors()
        .forEach(
            arguments -> {
              var expectedExtractor = (Function<ScopeResponse, String>) arguments.get()[0];
              var actualExtractor = (Function<ScopeResponse, String>) arguments.get()[1];
              assertEquals(
                  expectedExtractor.apply(scopeResponse), actualExtractor.apply(firstResponse));
            });
  }

  static Stream<Arguments> scopeFoundCases() {
    return Stream.of(
        Arguments.of(
            "test-scope",
            (Function<ScopeResponse, String>) ScopeResponse::getName,
            (Function<ScopeResponse, String>) ScopeResponse::getName),
        Arguments.of(
            "test-scope",
            (Function<ScopeResponse, String>) ScopeResponse::getGroup,
            (Function<ScopeResponse, String>) ScopeResponse::getGroup),
        Arguments.of(
            "test-scope",
            (Function<ScopeResponse, String>) ScopeResponse::getType,
            (Function<ScopeResponse, String>) ScopeResponse::getType),
        Arguments.of(
            "other-scope",
            (Function<ScopeResponse, String>) ScopeResponse::getName,
            (Function<ScopeResponse, String>) ScopeResponse::getName),
        Arguments.of(
            "other-scope",
            (Function<ScopeResponse, String>) ScopeResponse::getGroup,
            (Function<ScopeResponse, String>) ScopeResponse::getGroup),
        Arguments.of(
            "other-scope",
            (Function<ScopeResponse, String>) ScopeResponse::getType,
            (Function<ScopeResponse, String>) ScopeResponse::getType));
  }

  static Stream<Arguments> scopeFieldExtractors() {
    return Stream.of(
        Arguments.of(
            (Function<ScopeResponse, String>) ScopeResponse::getName,
            (Function<ScopeResponse, String>) ScopeResponse::getName),
        Arguments.of(
            (Function<ScopeResponse, String>) ScopeResponse::getGroup,
            (Function<ScopeResponse, String>) ScopeResponse::getGroup),
        Arguments.of(
            (Function<ScopeResponse, String>) ScopeResponse::getType,
            (Function<ScopeResponse, String>) ScopeResponse::getType));
  }
}
