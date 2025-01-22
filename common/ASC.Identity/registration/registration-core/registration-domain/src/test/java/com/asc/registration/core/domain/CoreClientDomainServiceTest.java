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
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientWebsiteInfo;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;

class CoreClientDomainServiceTest {
  private CoreClientDomainService service;
  private Audit audit;
  private Client client;

  @BeforeEach
  void setUp() {
    service = new CoreClientDomainService();
    audit = mock(Audit.class);
    client = mock(Client.class);

    when(audit.getUserEmail()).thenReturn("test@example.com");
  }

  @Test
  void whenClientIsCreated_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.createClient(audit, client);

    verify(client).initialize(captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientIsEnabled_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.enableClient(audit, client);

    verify(client).enable(captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientIsDisabled_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.disableClient(audit, client);

    verify(client).disable(captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientIsDeleted_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.deleteClient(audit, client);

    verify(client).disable(captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientSecretIsRegenerated_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.regenerateClientSecret(audit, client);

    verify(client).regenerateSecret(captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientInfoIsUpdated_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var clientInfo = new ClientInfo("Updated Client", "Updated Description", "Updated Logo URL");
    var event = service.updateClientInfo(audit, client, clientInfo);

    verify(client).updateClientInfo(eq(clientInfo), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientWebsiteInfoIsUpdated_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var clientWebsiteInfo =
        ClientWebsiteInfo.Builder.builder()
            .websiteUrl("http://updated.website")
            .termsUrl("http://updated.terms")
            .policyUrl("http://updated.policy")
            .build();
    var event = service.updateClientWebsiteInfo(audit, client, clientWebsiteInfo);

    verify(client).updateClientWebsiteInfo(eq(clientWebsiteInfo), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientRedirectInfoIsUpdated_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var clientRedirectInfo =
        new ClientRedirectInfo(
            Set.of("http://updated.redirect"),
            Set.of("http://updated.origin"),
            Set.of("http://updated.logout"));
    var event = service.updateClientRedirectInfo(audit, client, clientRedirectInfo);

    verify(client).updateClientRedirectInfo(eq(clientRedirectInfo), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenAuthenticationMethodIsAdded_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var method = AuthenticationMethod.DEFAULT_AUTHENTICATION;
    var event = service.addAuthenticationMethod(audit, client, method);

    verify(client).addAuthenticationMethod(eq(method), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenAuthenticationMethodIsRemoved_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var method = AuthenticationMethod.DEFAULT_AUTHENTICATION;
    var event = service.removeAuthenticationMethod(audit, client, method);

    verify(client).removeAuthenticationMethod(eq(method), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenScopeIsAdded_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var scope = "newScope";
    var event = service.addScope(audit, client, scope);

    verify(client).addScope(eq(scope), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenScopeIsRemoved_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var scope = "existingScope";
    var event = service.removeScope(audit, client, scope);

    verify(client).removeScope(eq(scope), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientIsMadePublic_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.makeClientPublic(audit, client);

    verify(client).changeVisibility(eq(ClientVisibility.PUBLIC), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void whenClientIsMadePrivate_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.makeClientPrivate(audit, client);

    verify(client).changeVisibility(eq(ClientVisibility.PRIVATE), captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }
}
