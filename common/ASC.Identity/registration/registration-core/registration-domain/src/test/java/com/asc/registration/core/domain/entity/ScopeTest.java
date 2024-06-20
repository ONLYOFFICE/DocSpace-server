package com.asc.registration.core.domain.entity;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.registration.core.domain.exception.ScopeDomainException;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ScopeTest {
  private Scope scope;

  @BeforeEach
  void setUp() {
    scope = Scope.Builder.builder().name("read").group("user").type("permission").build();
  }

  @Test
  void testCreateScope() {
    assertNotNull(scope);
    assertEquals("read", scope.getName());
    assertEquals("user", scope.getGroup());
    assertEquals("permission", scope.getType());
  }

  @Test
  void testUpdateGroup() {
    scope.updateGroup("admin");
    assertEquals("admin", scope.getGroup());
  }

  @Test
  void testUpdateType() {
    scope.updateType("role");
    assertEquals("role", scope.getType());
  }

  @Test
  void testValidate() {
    var exception =
        assertThrows(
            ScopeDomainException.class,
            () -> {
              Scope invalidScope =
                  Scope.Builder.builder().name("").group("user").type("permission").build();
            });
    assertEquals("Scope name must not be null or empty", exception.getMessage());

    exception =
        assertThrows(
            ScopeDomainException.class,
            () -> {
              Scope invalidScope =
                  Scope.Builder.builder().name("read").group("").type("permission").build();
            });
    assertEquals("Scope group must not be null or empty", exception.getMessage());

    exception =
        assertThrows(
            ScopeDomainException.class,
            () -> {
              Scope invalidScope =
                  Scope.Builder.builder().name("read").group("user").type("").build();
            });
    assertEquals("Scope type must not be null or empty", exception.getMessage());
  }
}
