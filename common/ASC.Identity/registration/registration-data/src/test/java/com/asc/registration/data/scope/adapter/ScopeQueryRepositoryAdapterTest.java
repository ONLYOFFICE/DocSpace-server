package com.asc.registration.data.scope.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.*;

import com.asc.common.data.scope.entity.ScopeEntity;
import com.asc.common.data.scope.repository.JpaScopeRepository;
import com.asc.registration.core.domain.entity.Scope;
import com.asc.registration.data.scope.mapper.ScopeDataAccessMapper;
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
  void testFindByName() {
    when(jpaScopeRepository.findById("testScope")).thenReturn(Optional.of(scopeEntity));

    var result = scopeQueryRepositoryAdapter.findByName("testScope");

    verify(jpaScopeRepository, times(1)).findById("testScope");
    verify(scopeDataAccessMapper, times(1)).toDomain(any(ScopeEntity.class));

    assertTrue(result.isPresent());
    assertEquals("testScope", result.get().getName());
  }

  @Test
  void testFindAll() {
    when(jpaScopeRepository.findAll()).thenReturn(Collections.singletonList(scopeEntity));

    var result = scopeQueryRepositoryAdapter.findAll();

    verify(jpaScopeRepository, times(1)).findAll();
    verify(scopeDataAccessMapper, times(1)).toDomain(any(ScopeEntity.class));

    assertEquals(1, result.size());
    assertEquals("testScope", result.iterator().next().getName());
  }
}
