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
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
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

  @Test
  public void testGetScope() {
    when(queryRepository.findByName(anyString())).thenReturn(Optional.of(scope));
    when(dataMapper.toScopeResponse(any(Scope.class))).thenReturn(scopeResponse);

    var response = scopeQueryHandler.getScope("test-scope");

    verify(queryRepository, times(1)).findByName("test-scope");
    verify(dataMapper, times(1)).toScopeResponse(any(Scope.class));

    assertEquals(scopeResponse.getName(), response.getName());
    assertEquals(scopeResponse.getGroup(), response.getGroup());
    assertEquals(scopeResponse.getType(), response.getType());
  }

  @Test
  public void testGetScopeNotFound() {
    when(queryRepository.findByName(anyString())).thenReturn(Optional.empty());

    assertThrows(ScopeNotFoundException.class, () -> scopeQueryHandler.getScope("test-scope"));

    verify(queryRepository, times(1)).findByName("test-scope");
    verify(dataMapper, times(0)).toScopeResponse(any(Scope.class));
  }

  @Test
  public void testGetScopes() {
    when(queryRepository.findAll()).thenReturn(Collections.singleton(scope));
    when(dataMapper.toScopeResponse(any(Scope.class))).thenReturn(scopeResponse);

    var response = scopeQueryHandler.getScopes();

    verify(queryRepository, times(1)).findAll();
    verify(dataMapper, times(1)).toScopeResponse(any(Scope.class));

    assertEquals(1, response.size());
    assertEquals(scopeResponse.getName(), response.iterator().next().getName());
    assertEquals(scopeResponse.getGroup(), response.iterator().next().getGroup());
    assertEquals(scopeResponse.getType(), response.iterator().next().getType());
  }
}
