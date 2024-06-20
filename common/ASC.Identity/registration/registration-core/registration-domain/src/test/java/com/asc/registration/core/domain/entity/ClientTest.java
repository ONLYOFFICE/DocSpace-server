package com.asc.registration.core.domain.entity;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.exception.ClientDomainException;
import com.asc.registration.core.domain.value.*;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ClientTest {
  private Client client;

  @BeforeEach
  void setUp() {
    client =
        Client.Builder.builder()
            .id(new ClientId(UUID.randomUUID()))
            .secret(new ClientSecret(UUID.randomUUID().toString()))
            .authenticationMethods(Set.of(AuthenticationMethod.DEFAULT_AUTHENTICATION))
            .scopes(Set.of("read", "write"))
            .clientInfo(new ClientInfo("Test Client", "Description", "Logo URL"))
            .clientTenantInfo(new ClientTenantInfo(new TenantId(1)))
            .clientRedirectInfo(
                new ClientRedirectInfo(
                    Set.of("http://redirect.url"),
                    Set.of("http://allowed.origin"),
                    Set.of("http://logout.url")))
            .clientCreationInfo(
                ClientCreationInfo.Builder.builder()
                    .createdBy("creator")
                    .createdOn(ZonedDateTime.now(ZoneId.of("UTC")))
                    .build())
            .clientVisibility(ClientVisibility.PRIVATE)
            .build();
  }

  @Test
  void testInitialize() {
    client.initialize("creator@example.com");

    assertNotNull(client.getId());
    assertNotNull(client.getSecret());
    assertEquals(ClientStatus.ENABLED, client.getStatus());
    assertNotNull(client.getClientCreationInfo());
    assertEquals("creator@example.com", client.getClientCreationInfo().getCreatedBy());
    assertNotNull(client.getClientCreationInfo().getCreatedOn());
  }

  @Test
  void testEnable() {
    client.initialize("creator@example.com");
    client.disable("modifier@example.com");
    client.enable("modifier@example.com");

    assertEquals(ClientStatus.ENABLED, client.getStatus());
  }

  @Test
  void testDisable() {
    client.initialize("creator@example.com");
    client.disable("modifier@example.com");

    assertEquals(ClientStatus.DISABLED, client.getStatus());
  }

  @Test
  void testInvalidate() {
    client.initialize("creator@example.com");
    client.invalidate("modifier@example.com");

    assertEquals(ClientStatus.INVALIDATED, client.getStatus());
  }

  @Test
  void testInvalidateRegeneratesSecret() {
    client.initialize("creator@example.com");
    String oldSecret = client.getSecret().value();
    client.invalidate("modifier@example.com");

    assertNotEquals(oldSecret, client.getSecret().value());
    assertEquals(ClientStatus.INVALIDATED, client.getStatus());
  }

  @Test
  void testRegenerateSecret() {
    client.initialize("creator@example.com");
    client.disable("modifier@example.com");
    String oldSecret = client.getSecret().value();
    client.regenerateSecret("modifier@example.com");

    assertNotEquals(oldSecret, client.getSecret().value());
  }

  @Test
  void testAddScope() {
    client.initialize("creator@example.com");
    String newScope = "delete";
    client.addScope(newScope, "modifier@example.com");

    assertTrue(client.getScopes().contains(newScope));
  }

  @Test
  void testRemoveScope() {
    client.initialize("creator@example.com");
    String scopeToRemove = "read";
    client.removeScope(scopeToRemove, "modifier@example.com");

    assertFalse(client.getScopes().contains(scopeToRemove));
    assertTrue(client.getScopes().contains("write"));
  }

  @Test
  void testRemoveLastScopeThrowsException() {
    client.initialize("creator@example.com");
    client.removeScope("read", "modifier@example.com");

    ClientDomainException exception =
        assertThrows(
            ClientDomainException.class,
            () -> {
              client.removeScope("write", "modifier@example.com");
            });

    assertEquals("Client must have at least one scope", exception.getMessage());
  }

  @Test
  void testChangeVisibility() {
    client.initialize("creator@example.com");
    client.changeVisibility(ClientVisibility.PUBLIC, "modifier@example.com");

    assertEquals(ClientVisibility.PUBLIC, client.getVisibility());
  }

  @Test
  void testUpdateClientInfo() {
    client.initialize("creator@example.com");
    var newClientInfo = new ClientInfo("Updated Client", "Updated Description", "Updated Logo URL");
    client.updateClientInfo(newClientInfo, "modifier@example.com");

    assertEquals(newClientInfo, client.getClientInfo());
  }

  @Test
  void testUpdateClientWebsiteInfo() {
    client.initialize("creator@example.com");
    var newClientWebsiteInfo =
        ClientWebsiteInfo.Builder.builder()
            .websiteUrl("http://updated.url")
            .termsUrl("http://updated.url/terms")
            .policyUrl("http://updated.url/policy")
            .build();
    client.updateClientWebsiteInfo(newClientWebsiteInfo, "modifier@example.com");

    assertEquals(newClientWebsiteInfo, client.getClientWebsiteInfo());
  }

  @Test
  void testUpdateClientRedirectInfo() {
    client.initialize("creator@example.com");
    var newClientRedirectInfo =
        new ClientRedirectInfo(
            Set.of("http://updated.redirect.url"),
            Set.of("http://updated.allowed.origin"),
            Set.of("http://updated.logout.url"));
    client.updateClientRedirectInfo(newClientRedirectInfo, "modifier@example.com");

    assertEquals(newClientRedirectInfo, client.getClientRedirectInfo());
  }

  @Test
  void testAddAuthenticationMethod() {
    client.initialize("creator@example.com");
    var newMethod = AuthenticationMethod.PKCE_AUTHENTICATION;
    client.addAuthenticationMethod(newMethod, "modifier@example.com");

    assertTrue(client.getAuthenticationMethods().contains(newMethod));
  }

  @Test
  void testRemoveAuthenticationMethod() {
    client.initialize("creator@example.com");
    var methodToRemove = AuthenticationMethod.DEFAULT_AUTHENTICATION;
    var newMethod = AuthenticationMethod.PKCE_AUTHENTICATION;
    client.addAuthenticationMethod(newMethod, "modifier@example.com");
    client.removeAuthenticationMethod(methodToRemove, "modifier@example.com");

    assertFalse(client.getAuthenticationMethods().contains(methodToRemove));
    assertTrue(client.getAuthenticationMethods().contains(newMethod));
  }

  @Test
  void testRemoveLastAuthenticationMethodThrowsException() {
    client.initialize("creator@example.com");

    var exception =
        assertThrows(
            ClientDomainException.class,
            () -> {
              client.removeAuthenticationMethod(
                  AuthenticationMethod.DEFAULT_AUTHENTICATION, "modifier@example.com");
            });

    assertEquals(
        "Client must have at least one authentication method. Cannot remove the last one",
        exception.getMessage());
  }
}
