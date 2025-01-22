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
  void whenScopeIsCreated_thenEventIsGenerated() {
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
  void whenScopeGroupIsUpdated_thenEventIsGenerated() {
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
  void whenScopeTypeIsUpdated_thenEventIsGenerated() {
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
  void whenScopeIsDeleted_thenEventIsGenerated() {
    var event = service.deleteScope(audit, scope);

    assertNotNull(event);
    assertEquals("read", event.getScope().getName());
    assertNotNull(event.getEventAt());
  }
}
