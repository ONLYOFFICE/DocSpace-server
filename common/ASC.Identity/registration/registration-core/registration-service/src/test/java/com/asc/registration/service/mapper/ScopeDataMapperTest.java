package com.asc.registration.service.mapper;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.registration.core.domain.entity.Scope;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ScopeDataMapperTest {
  private ScopeDataMapper scopeDataMapper;

  @BeforeEach
  void setUp() {
    scopeDataMapper = new ScopeDataMapper();
  }

  @Test
  void testToScopeResponse() {
    var scope = createScope();
    var response = scopeDataMapper.toScopeResponse(scope);

    assertNotNull(response);
    assertEquals(scope.getName(), response.getName());
    assertEquals(scope.getGroup(), response.getGroup());
    assertEquals(scope.getType(), response.getType());
  }

  @Test
  void testToScopeResponse_NullScope() {
    var exception =
        assertThrows(IllegalArgumentException.class, () -> scopeDataMapper.toScopeResponse(null));

    assertEquals("Scope cannot be null", exception.getMessage());
  }

  private Scope createScope() {
    return Scope.Builder.builder().name("test-scope").group("test-group").type("test-type").build();
  }
}
