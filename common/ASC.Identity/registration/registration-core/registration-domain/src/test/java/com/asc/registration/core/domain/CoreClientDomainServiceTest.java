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
  void testCreateClient() {
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
  void testEnableClient() {
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
  void testDisableClient() {
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
  void testInvalidateClient() {
    var captor = ArgumentCaptor.forClass(String.class);
    var event = service.invalidateClient(audit, client);

    verify(client).invalidate(captor.capture());

    assertEquals("test@example.com", captor.getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testRegenerateClientSecret() {
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
  void testUpdateClientInfo() {
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
  void testUpdateClientWebsiteInfo() {
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
  void testUpdateClientRedirectInfo() {
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
  void testAddAuthenticationMethod() {
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
  void testRemoveAuthenticationMethod() {
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
  void testAddScope() {
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
  void testRemoveScope() {
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
  void testMakeClientPublic() {
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
  void testMakeClientPrivate() {
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
