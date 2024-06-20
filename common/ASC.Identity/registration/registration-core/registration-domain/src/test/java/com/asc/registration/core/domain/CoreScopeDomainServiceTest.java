package com.asc.registration.core.domain;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.core.domain.entity.Scope;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;

class CoreScopeDomainServiceTest {
  private CoreScopeDomainService service;
  private Audit audit;
  private Scope scope;

  @BeforeEach
  void setUp() {
    service = new CoreScopeDomainService();
    audit = mock(Audit.class);
    scope = mock(Scope.class);

    when(audit.getUserEmail()).thenReturn("test@example.com");
    when(scope.getName()).thenReturn("read");
    when(scope.getGroup()).thenReturn("user");
    when(scope.getType()).thenReturn("permission");
  }

  @Test
  void testCreateScope() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.createScope(audit, scope);

    verify(scope, times(1)).getName();
    verify(scope, times(1)).getGroup();
    verify(scope, times(1)).getType();

    assertNotNull(event);
    assertEquals("read", event.getScope().getName());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testUpdateScopeGroup() {
    var captor = ArgumentCaptor.forClass(String.class);

    when(scope.getGroup()).thenReturn("admin");

    var event = service.updateScopeGroup(audit, scope, "admin");

    verify(scope).updateGroup(captor.capture());

    assertEquals("admin", captor.getValue());
    assertNotNull(event);
    assertEquals("read", event.getScope().getName());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testUpdateScopeType() {
    var captor = ArgumentCaptor.forClass(String.class);

    when(scope.getType()).thenReturn("role");

    var event = service.updateScopeType(audit, scope, "role");

    verify(scope).updateType(captor.capture());

    assertEquals("role", captor.getValue());
    assertNotNull(event);
    assertEquals("read", event.getScope().getName());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testDeleteScope() {
    var event = service.deleteScope(audit, scope);

    assertNotNull(event);
    assertEquals("read", event.getScope().getName());
    assertNotNull(event.getEventAt());
  }
}
