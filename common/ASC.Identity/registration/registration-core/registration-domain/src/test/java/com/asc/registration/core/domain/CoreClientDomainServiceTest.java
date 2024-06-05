package com.asc.registration.core.domain;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientCreatedEvent;
import com.asc.registration.core.domain.event.ClientDeletedEvent;
import com.asc.registration.core.domain.event.ClientUpdatedEvent;
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
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);

    ClientCreatedEvent event = service.createClient(audit, client);

    verify(client).initialize(captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testEnableClient() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);

    ClientUpdatedEvent event = service.enableClient(audit, client);

    verify(client).enable(captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testDisableClient() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);

    ClientUpdatedEvent event = service.disableClient(audit, client);

    verify(client).disable(captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testInvalidateClient() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);

    ClientDeletedEvent event = service.invalidateClient(audit, client);

    verify(client).invalidate(captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testRegenerateClientSecret() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);

    ClientUpdatedEvent event = service.regenerateClientSecret(audit, client);

    verify(client).regenerateSecret(captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testUpdateClientInfo() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);
    ClientInfo clientInfo =
        new ClientInfo("Updated Client", "Updated Description", "Updated Logo URL");

    ClientUpdatedEvent event = service.updateClientInfo(audit, client, clientInfo);

    verify(client).updateClientInfo(eq(clientInfo), captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testUpdateClientWebsiteInfo() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);
    ClientWebsiteInfo clientWebsiteInfo =
        ClientWebsiteInfo.Builder.builder()
            .websiteUrl("http://updated.website")
            .termsUrl("http://updated.terms")
            .policyUrl("http://updated.policy")
            .build();

    ClientUpdatedEvent event = service.updateClientWebsiteInfo(audit, client, clientWebsiteInfo);

    verify(client).updateClientWebsiteInfo(eq(clientWebsiteInfo), captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testUpdateClientRedirectInfo() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);
    ClientRedirectInfo clientRedirectInfo =
        new ClientRedirectInfo(
            Set.of("http://updated.redirect"),
            Set.of("http://updated.origin"),
            Set.of("http://updated.logout"));

    ClientUpdatedEvent event = service.updateClientRedirectInfo(audit, client, clientRedirectInfo);

    verify(client).updateClientRedirectInfo(eq(clientRedirectInfo), captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testAddAuthenticationMethod() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);
    AuthenticationMethod method = AuthenticationMethod.DEFAULT_AUTHENTICATION;

    ClientUpdatedEvent event = service.addAuthenticationMethod(audit, client, method);

    verify(client).addAuthenticationMethod(eq(method), captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testRemoveAuthenticationMethod() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);
    AuthenticationMethod method = AuthenticationMethod.DEFAULT_AUTHENTICATION;

    ClientUpdatedEvent event = service.removeAuthenticationMethod(audit, client, method);

    verify(client).removeAuthenticationMethod(eq(method), captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testAddScope() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);
    String scope = "newScope";

    ClientUpdatedEvent event = service.addScope(audit, client, scope);

    verify(client).addScope(eq(scope), captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @Test
  void testRemoveScope() {
    ArgumentCaptor<String> captor = ArgumentCaptor.forClass(String.class);
    String scope = "existingScope";

    ClientUpdatedEvent event = service.removeScope(audit, client, scope);

    verify(client).removeScope(eq(scope), captor.capture());
    assertEquals("test@example.com", captor.getValue());

    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }
}
